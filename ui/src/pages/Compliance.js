import { AlertCircle, AlertTriangle, CheckCircle2, Clock, FileCheck, FileSpreadsheet, FileText, Filter, RefreshCw, Shield, ShieldCheck, Sparkles } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError } from "../api/client.js";
import { complianceApi } from "../api/compliance.js";
import { aiApi } from "../api/ai.js";
import PageTitle from "../components/PageTitle.js";
import { t } from "../i18n.js";
import { useAuth } from "../state/auth.js";

const RISK_VARIANT = { high: "danger", medium: "warning", low: "info", none: "success" };

function RiskIcon({ level }) {
  if (level === "high") return <AlertCircle size={14} className="text-danger" />;
  if (level === "medium") return <AlertTriangle size={14} className="text-warning" />;
  if (level === "low") return <Clock size={14} className="text-info" />;
  return <CheckCircle2 size={14} className="text-success" />;
}

function ReadinessCard({ label, ready, notReady, icon: Icon }) {
  const total = ready + notReady;
  const pct = total > 0 ? Math.round((ready / total) * 100) : 0;
  const color = pct >= 80 ? "success" : pct >= 50 ? "warning" : "danger";
  return (
    <div className="col-md-6 col-xl-3">
      <div className="card ops-card">
        <div className="card-body">
          <div className="d-flex align-items-center mb-3">
            <Icon size={16} className={`text-${color} mr-2`} />
            <span className="font-weight-semibold">{label}</span>
            <span className="ml-auto font-size-13 text-muted">{pct}%</span>
          </div>
          <div className="progress progress-sm mb-2">
            <div className={`progress-bar bg-${color}`} style={{ width: `${pct}%` }} />
          </div>
          <div className="d-flex justify-content-between">
            <small className="text-success">{ready} hazır</small>
            <small className="text-danger">{notReady} eksik</small>
          </div>
        </div>
      </div>
    </div>
  );
}

function TimelinePanel({ timeline, language, onClose }) {
  const eventDot = (type) => {
    const colors = { issued: "primary", sent: "info", archived: "dark", dunning: "warning", paid: "success", cancelled: "danger" };
    return <span className={`status-pill status-${colors[type] || "muted"}`} style={{ width: 10, height: 10, padding: 0, borderRadius: "50%", display: "inline-block" }} />;
  };

  return (
    <div className="card ops-card">
      <div className="card-body">
        <div className="settings-section-header">
          <div>
            <h4 className="card-title mb-1">{t(language, "complianceAuditTimeline")}</h4>
            <p className="text-muted mb-0 font-size-12">{timeline.invoiceNumber || "—"} · {timeline.status}</p>
          </div>
          <button className="btn btn-sm btn-light" onClick={onClose}>✕</button>
        </div>
        <p className="text-muted font-size-12 mt-2 mb-3">{t(language, "complianceAuditTimelineHint")}</p>
        {timeline.events.length === 0 ? (
          <p className="text-muted">{t(language, "noComplianceEvents")}</p>
        ) : (
          <ul className="list-unstyled">
            {timeline.events.map((ev, i) => (
              <li key={i} className="d-flex align-items-start mb-3">
                <div className="mr-3 mt-1">{eventDot(ev.eventType)}</div>
                <div>
                  <div className="font-size-13 font-weight-semibold">
                    {t(language, `complianceEvent_${ev.description}`) || ev.description}
                  </div>
                  <div className="text-muted font-size-12">{new Date(ev.occurredAtUtc).toLocaleString()}</div>
                  {ev.actorEmail && <div className="text-muted font-size-11">{ev.actorEmail}</div>}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

export default function Compliance() {
  const { language, canWrite } = useAuth();
  const navigate = useNavigate();
  const [dashboard, setDashboard] = useState(null);
  const [timeline, setTimeline] = useState(null);
  const [loading, setLoading] = useState(true);
  const [timelineLoading, setTimelineLoading] = useState(false);
  const [message, setMessage] = useState(null);
  const [filters, setFilters] = useState({ from: "", to: "", status: "", riskLevel: "" });
  const [activeFilters, setActiveFilters] = useState({});
  const [fieldSuggestionsByInvoice, setFieldSuggestionsByInvoice] = useState({});

  async function load(appliedFilters = activeFilters) {
    setLoading(true);
    setMessage(null);
    try {
      const data = await complianceApi.dashboard(appliedFilters);
      setDashboard(data);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  async function loadTimeline(invoiceDraftId) {
    setTimelineLoading(true);
    try {
      const data = await complianceApi.timeline(invoiceDraftId);
      setTimeline(data);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Timeline load failed"]);
    } finally {
      setTimelineLoading(false);
    }
  }

  function applyFilters(e) {
    e.preventDefault();
    const applied = {
      from: filters.from || undefined,
      to: filters.to || undefined,
      status: filters.status || undefined,
      riskLevel: filters.riskLevel || undefined,
    };
    setActiveFilters(applied);
    load(applied);
  }

  function clearFilters() {
    setFilters({ from: "", to: "", status: "", riskLevel: "" });
    setActiveFilters({});
    load({});
  }

  useEffect(() => {
    let alive = true;
    complianceApi.dashboard({})
      .then((data) => { if (alive) setDashboard(data); })
      .catch((err) => { if (alive) setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]); })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, []);

  useEffect(() => {
    if (!dashboard) return;
    const targets = (dashboard.documents || [])
      .filter((d) => d.riskLevel === "high" || d.riskLevel === "medium")
      .slice(0, 25);
    if (targets.length === 0) return;
    let alive = true;
    Promise.all(targets.map((d) =>
      aiApi.suggestInvoiceFields(d.invoiceDraftId)
        .then((items) => [d.invoiceDraftId, Array.isArray(items) ? items : []])
        .catch(() => [d.invoiceDraftId, []])
    )).then((pairs) => {
      if (!alive) return;
      const next = {};
      for (const [id, items] of pairs) next[id] = items;
      setFieldSuggestionsByInvoice(next);
    });
    return () => { alive = false; };
  }, [dashboard]);

  async function applyFieldSuggestion(invoiceDraftId, suggestion) {
    try {
      await aiApi.log({
        suggestionType: "InvoiceField",
        targetType: "Invoice",
        targetId: invoiceDraftId,
        payload: suggestion
      }, "Accepted");
    } catch { /* ignore */ }
    navigate(`/invoices/${invoiceDraftId}`);
  }

  const readiness = dashboard?.readiness;
  const documents = dashboard?.documents || [];

  return (
    <>
      <PageTitle
        title={t(language, "compliance")}
        action={<button className="btn btn-secondary" type="button" onClick={() => load()}><RefreshCw size={15} /> {t(language, "refresh")}</button>}
      />

      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}

      {/* Summary tiles */}
      <div className="ops-summary-grid">
        <div className="summary-tile">
          <span><FileText size={14} /> {t(language, "complianceTotalDocuments")}</span>
          <strong>{loading ? "—" : (dashboard?.totalInvoices ?? 0)}</strong>
        </div>
        <div className="summary-tile">
          <span><AlertTriangle size={14} /> {t(language, "complianceRiskyDocuments")}</span>
          <strong>{loading ? "—" : (dashboard?.riskyInvoices ?? 0)}</strong>
        </div>
        <div className="summary-tile">
          <span><AlertCircle size={14} /> {t(language, "complianceHighRisk")}</span>
          <strong className={dashboard?.highRiskInvoices > 0 ? "text-danger" : ""}>{loading ? "—" : (dashboard?.highRiskInvoices ?? 0)}</strong>
        </div>
        <div className="summary-tile">
          <span><Shield size={14} /> {t(language, "complianceMahnwesenRisk")}</span>
          <strong>{loading ? "—" : (readiness?.mahnwesenOverdueRisk ?? 0)}</strong>
        </div>
      </div>

      {/* Readiness cards */}
      {readiness && (
        <>
          <div className="d-flex align-items-baseline mb-2 mt-3">
            <h5 className="mb-0 mr-2">{t(language, "complianceReadiness")}</h5>
            <span className="text-muted font-size-13">{t(language, "complianceReadinessHint")}</span>
          </div>
          <div className="row mb-3">
            <ReadinessCard label="XRechnung" ready={readiness.xRechnungReady} notReady={readiness.xRechnungNotReady} icon={FileText} />
            <ReadinessCard label="ZUGFeRD" ready={readiness.zugferdReady} notReady={readiness.zugferdNotReady} icon={FileCheck} />
            <ReadinessCard label="GoBD" ready={readiness.gobdArchived} notReady={readiness.gobdNotArchived} icon={ShieldCheck} />
            <ReadinessCard label="DATEV" ready={readiness.datevExported} notReady={readiness.datevNotExported} icon={FileSpreadsheet} />
            <ReadinessCard label={t(language, "complianceSchematron")} ready={readiness.schematronReady} notReady={readiness.schematronNotReady} icon={ShieldCheck} />
            <ReadinessCard label={t(language, "complianceDispatched")} ready={readiness.dispatched} notReady={readiness.notDispatched} icon={FileCheck} />
          </div>
        </>
      )}

      <div className="row">
        <div className={timeline ? "col-xl-8" : "col-12"}>

          {/* Filters */}
          <div className="card ops-card">
            <div className="card-body py-2">
              <form className="row align-items-end g-2" onSubmit={applyFilters}>
                <div className="col-md-3 col-sm-6">
                  <label className="form-label font-size-12 mb-1">{t(language, "fromDate")}</label>
                  <input type="date" className="form-control form-control-sm" value={filters.from} onChange={(e) => setFilters({ ...filters, from: e.target.value })} />
                </div>
                <div className="col-md-3 col-sm-6">
                  <label className="form-label font-size-12 mb-1">{t(language, "toDate")}</label>
                  <input type="date" className="form-control form-control-sm" value={filters.to} onChange={(e) => setFilters({ ...filters, to: e.target.value })} />
                </div>
                <div className="col-md-2 col-sm-6">
                  <label className="form-label font-size-12 mb-1">{t(language, "status")}</label>
                  <select className="form-control form-control-sm" value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value })}>
                    <option value="">{t(language, "allStatuses")}</option>
                    <option value="Draft">{t(language, "invoiceStatusDraft")}</option>
                    <option value="Issued">{t(language, "invoiceStatusIssued")}</option>
                    <option value="Sent">{t(language, "invoiceStatusSent")}</option>
                    <option value="PartiallyPaid">{t(language, "invoiceStatusPartiallyPaid")}</option>
                    <option value="Paid">{t(language, "invoiceStatusPaid")}</option>
                    <option value="Overdue">{t(language, "invoiceStatusOverdue")}</option>
                    <option value="Cancelled">{t(language, "invoiceStatusCancelled")}</option>
                  </select>
                </div>
                <div className="col-md-2 col-sm-6">
                  <label className="form-label font-size-12 mb-1">{t(language, "complianceRiskLevel")}</label>
                  <select className="form-control form-control-sm" value={filters.riskLevel} onChange={(e) => setFilters({ ...filters, riskLevel: e.target.value })}>
                    <option value="">{t(language, "allRisks")}</option>
                    <option value="high">{t(language, "complianceRisk_high")}</option>
                    <option value="medium">{t(language, "complianceRisk_medium")}</option>
                    <option value="low">{t(language, "complianceRisk_low")}</option>
                    <option value="none">{t(language, "complianceRisk_none")}</option>
                  </select>
                </div>
                <div className="col-md-2 col-sm-12">
                  <div className="d-flex gap-1">
                    <button type="submit" className="btn btn-primary btn-sm"><Filter size={14} /> {t(language, "filter")}</button>
                    <button type="button" className="btn btn-light btn-sm" onClick={clearFilters}>{t(language, "clear")}</button>
                  </div>
                </div>
              </form>
            </div>
          </div>

          {/* Documents table */}
          <div className="card ops-card">
            <div className="card-body">
              <div className="settings-section-header">
                <div>
                  <h4 className="card-title mb-1">{t(language, "complianceDocumentRisk")}</h4>
                  <p className="text-muted mb-0">{t(language, "complianceDocumentRiskHint")}</p>
                </div>
              </div>
              {loading ? (
                <div className="text-muted mt-3">{t(language, "loading")}...</div>
              ) : documents.length === 0 ? (
                <div className="text-muted mt-3">{t(language, "noComplianceDocuments")}</div>
              ) : (
                <div className="table-responsive mt-3">
                  <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                    <thead>
                      <tr>
                        <th>{t(language, "invoiceNumber")}</th>
                        <th>{t(language, "customer")}</th>
                        <th>{t(language, "status")}</th>
                        <th>{t(language, "issueDate")}</th>
                        <th>GoBD</th>
                        <th>DATEV</th>
                        <th>XRechnung</th>
                        <th>{t(language, "complianceSchematron")}</th>
                        <th>{t(language, "complianceDispatched")}</th>
                        <th>{t(language, "complianceRiskLevel")}</th>
                        <th>{t(language, "aiSuggestedFix")}</th>
                        <th className="text-right">{t(language, "actions")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {documents.map((doc) => (
                        <tr key={doc.invoiceDraftId}>
                          <td>
                            <div className="entity-cell">
                              <span className={`entity-avatar avatar-product ${doc.riskLevel === "high" ? "text-danger" : doc.riskLevel === "medium" ? "text-warning" : "text-muted"}`}>
                                <RiskIcon level={doc.riskLevel} />
                              </span>
                              <span>
                                <Link to={`/invoices/${doc.invoiceDraftId}`} className="font-weight-semibold">
                                  {doc.invoiceNumber || "—"}
                                </Link>
                                {doc.daysOverdue > 0 && <small className="d-block text-danger">{doc.daysOverdue}g gecikti</small>}
                              </span>
                            </div>
                          </td>
                          <td className="font-size-13">{doc.customerName || "—"}</td>
                          <td><span className="status-pill status-muted">{doc.status}</span></td>
                          <td className="font-size-12">{doc.issueDate ? new Date(doc.issueDate).toLocaleDateString("de-DE") : "—"}</td>
                          <td>
                            {doc.isGobdArchived
                              ? <CheckCircle2 size={15} className="text-success" />
                              : <AlertCircle size={15} className="text-danger" />}
                          </td>
                          <td>
                            {doc.isDatevExported
                              ? <CheckCircle2 size={15} className="text-success" />
                              : <AlertCircle size={15} className="text-danger" />}
                          </td>
                          <td>
                            {doc.isXRechnungReady
                              ? <CheckCircle2 size={15} className="text-success" />
                              : <AlertCircle size={15} className="text-danger" />}
                          </td>
                          <td>
                            {doc.isSchematronValid
                              ? <CheckCircle2 size={15} className="text-success" />
                              : <span title={(doc.schematronFailureCodes || []).join(", ")}>
                                  <AlertCircle size={15} className="text-danger" />
                                </span>}
                          </td>
                          <td>
                            {doc.isDispatched
                              ? <span className="status-pill status-success" title={doc.dispatchStatus || ""}>{doc.dispatchStatus || "ok"}</span>
                              : <span className="text-muted font-size-12">{doc.dispatchStatus || "—"}</span>}
                          </td>
                          <td>
                            <span className={`status-pill status-${RISK_VARIANT[doc.riskLevel] || "muted"}`}>
                              {t(language, `complianceRisk_${doc.riskLevel}`)}
                            </span>
                          </td>
                          <td>
                            {(fieldSuggestionsByInvoice[doc.invoiceDraftId] || []).length === 0
                              ? <span className="text-muted font-size-12">—</span>
                              : (fieldSuggestionsByInvoice[doc.invoiceDraftId] || []).slice(0, 1).map((s, i) => (
                                <div key={i} className="d-flex align-items-center" style={{ gap: 6 }}>
                                  <Sparkles size={12} className="text-warning" />
                                  <small className="text-muted">
                                    <strong>{s.fieldCode}</strong> · {t(language, `aiFieldRationale_${s.rationale}`) || s.rationale}
                                  </small>
                                  <button type="button" className="btn btn-sm btn-light" disabled={!canWrite} onClick={() => applyFieldSuggestion(doc.invoiceDraftId, s)}>
                                    {t(language, "aiSuggestionApply")}
                                  </button>
                                </div>
                              ))}
                          </td>
                          <td className="text-right">
                            <div className="table-action-group justify-content-end">
                              <button
                                type="button"
                                className="btn btn-icon btn-soft-primary"
                                title={t(language, "complianceViewTimeline")}
                                onClick={() => loadTimeline(doc.invoiceDraftId)}
                              >
                                <Clock size={15} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>

          {/* Risk detail cards */}
          {documents.some((d) => d.riskLevel !== "none" && d.risks?.length > 0) && (
            <div className="card ops-card">
              <div className="card-body">
                <div className="settings-section-header">
                  <div>
                    <h4 className="card-title mb-1">{t(language, "complianceRiskDetails")}</h4>
                    <p className="text-muted mb-0">{t(language, "complianceRiskDetailsHint")}</p>
                  </div>
                </div>
                <div className="row mt-3">
                  {documents
                    .filter((d) => d.riskLevel !== "none" && d.risks?.length > 0)
                    .slice(0, 12)
                    .map((doc) => (
                      <div key={doc.invoiceDraftId} className="col-md-6 mb-3">
                        <div className={`border-left border-${RISK_VARIANT[doc.riskLevel]} pl-3`} style={{ borderLeft: `3px solid var(--bs-${RISK_VARIANT[doc.riskLevel]}, #ccc)`, paddingLeft: 10 }}>
                          <div className="d-flex align-items-center mb-1">
                            <RiskIcon level={doc.riskLevel} />
                            <span className="font-weight-semibold font-size-13 ml-1">{doc.invoiceNumber || doc.customerName || "—"}</span>
                          </div>
                          {doc.risks.map((risk) => (
                            <div key={risk} className="text-muted font-size-12">
                              · {t(language, `complianceRiskCode_${risk}`) || risk}
                            </div>
                          ))}
                        </div>
                      </div>
                    ))}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Timeline panel */}
        {(timeline || timelineLoading) && (
          <div className="col-xl-4">
            {timelineLoading ? (
              <div className="card ops-card"><div className="card-body text-muted">{t(language, "loading")}...</div></div>
            ) : (
              <TimelinePanel timeline={timeline} language={language} onClose={() => setTimeline(null)} />
            )}
          </div>
        )}
      </div>
    </>
  );
}
