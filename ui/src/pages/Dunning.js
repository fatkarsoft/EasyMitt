import { BellRing, CalendarClock, CheckCircle2, Euro, Mail, MailPlus, Search, Users } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import SendEmailModal from "../components/SendEmailModal.js";
import { ApiError } from "../api/client.js";
import { dunningApi } from "../api/dunning.js";
import { emailApi } from "../api/email.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

export default function Dunning() {
  const { language, canWrite } = useAuth();
  const [overview, setOverview] = useState(null);
  const [query, setQuery] = useState("");
  const [selected, setSelected] = useState(null);
  const [note, setNote] = useState("");
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [emailOpen, setEmailOpen] = useState(false);

  async function load() {
    setLoading(true);
    setMessage(null);
    try {
      const data = await dunningApi.overview();
      setOverview(data);
      setSelected((current) => {
        if (!current) return data.invoices?.[0] || null;
        return data.invoices?.find((invoice) => invoice.invoiceDraftId === current.invoiceDraftId) || data.invoices?.[0] || null;
      });
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let alive = true;
    dunningApi.overview()
      .then((data) => {
        if (!alive) return;
        setOverview(data);
        setSelected(data.invoices?.[0] || null);
      })
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  const customers = overview?.customers || [];
  const filteredInvoices = useMemo(() => {
    const invoices = overview?.invoices || [];
    const normalized = query.trim().toLowerCase();
    if (!normalized) return invoices;
    return invoices.filter((invoice) => [invoice.invoiceNumber, invoice.customerName, invoice.customerEmail]
      .filter(Boolean)
      .some((value) => value.toLowerCase().includes(normalized)));
  }, [overview, query]);

  async function recordReminder() {
    if (!selected) return;
    setSaving(true);
    setMessage(null);
    try {
      await dunningApi.createReminder({ invoiceDraftId: selected.invoiceDraftId, note });
      setNote("");
      setMessage(["success", t(language, "reminderRecorded")]);
      await load();
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Save failed"]);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <PageTitle title={t(language, "dunning")} action={<button className="btn btn-secondary" type="button" onClick={load}><Search size={16} /> {t(language, "refresh")}</button>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}

      <div className="ops-summary-grid">
        <SummaryTile icon={Euro} label={t(language, "totalOpenReceivables")} value={money(overview?.totalOpenAmount)} />
        <SummaryTile icon={CalendarClock} label={t(language, "overdueInvoices")} value={overview?.overdueInvoiceCount || 0} />
        <SummaryTile icon={BellRing} label={t(language, "reminderDue")} value={overview?.reminderDueCount || 0} />
        <SummaryTile icon={CheckCircle2} label={t(language, "collectedLast30Days")} value={money(overview?.collectedAmount)} />
      </div>

      <div className="row">
        <div className="col-xl-8">
          <div className="card ops-card">
            <div className="card-body">
              <div className="ops-toolbar">
                <div>
                  <h4 className="card-title mb-1">{t(language, "dunningInvoices")}</h4>
                  <p className="text-muted mb-0">{t(language, "dunningCenterHint")}</p>
                </div>
                <div className="filter-control">
                  <span className="filter-icon"><Search size={16} /></span>
                  <input className="form-control" value={query} onChange={(event) => setQuery(event.target.value)} placeholder={t(language, "search")} />
                  <button className="btn btn-secondary" type="button" onClick={() => setQuery("")}>{t(language, "clear")}</button>
                </div>
              </div>
              {loading ? <div className="text-muted">{t(language, "loading")}...</div> : filteredInvoices.length === 0 ? <div className="text-muted">{t(language, "noOverdueInvoices")}</div> : (
                <div className="table-responsive">
                  <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                    <thead>
                      <tr>
                        <th>{t(language, "invoiceNumber")}</th>
                        <th>{t(language, "customer")}</th>
                        <th>{t(language, "dueDate")}</th>
                        <th>{t(language, "openAmount")}</th>
                        <th>{t(language, "reminderLevel")}</th>
                        <th className="text-right">{t(language, "actions")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredInvoices.map((invoice) => (
                        <tr key={invoice.invoiceDraftId} className={selected?.invoiceDraftId === invoice.invoiceDraftId ? "table-active" : ""}>
                          <td>
                            <div className="entity-cell">
                              <span className="entity-avatar avatar-product"><BellRing size={18} /></span>
                              <span><strong>{invoice.invoiceNumber}</strong><small>{t(language, "daysOverdue")}: {invoice.daysOverdue}</small></span>
                            </div>
                          </td>
                          <td>{invoice.customerName}<small className="d-block text-muted">{invoice.customerEmail || "-"}</small></td>
                          <td>{invoice.dueDate}</td>
                          <td><strong>{money(invoice.openAmount)}</strong><small className="d-block text-muted">{t(language, "total")}: {money(invoice.totalAmount)}</small></td>
                          <td><span className={`status-pill ${invoice.reminderLevel > 1 ? "status-risk" : invoice.reminderLevel > 0 ? "status-info" : "status-muted"}`}>{reminderLevelLabel(language, invoice.reminderLevel)}</span></td>
                          <td className="text-right">
                            <div className="table-action-group justify-content-end">
                              <Link className="btn btn-icon btn-soft-primary" to={`/invoices/${invoice.invoiceDraftId}`} title={t(language, "open")}><Search size={15} /></Link>
                              <button className="btn btn-icon btn-soft-danger" type="button" disabled={!canWrite} onClick={() => setSelected(invoice)} title={t(language, "recordReminder")}><MailPlus size={15} /></button>
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

          <div className="card ops-card">
            <div className="card-body">
              <div className="settings-section-header">
                <div>
                  <h4 className="card-title mb-1">{t(language, "dunningCustomers")}</h4>
                  <p className="text-muted mb-0">{t(language, "customerReceivablesHint")}</p>
                </div>
              </div>
              {customers.length === 0 ? <div className="text-muted mt-3">{t(language, "noOverdueInvoices")}</div> : (
                <div className="datev-mapping-list">
                  {customers.map((customer) => (
                    <div className="dunning-customer-row" key={customer.customerId || customer.customerName}>
                      <div className="entity-cell">
                        <span className="entity-avatar"><Users size={18} /></span>
                        <span><strong>{customer.customerName}</strong><small>{customer.overdueInvoiceCount} {t(language, "overdueInvoices")}</small></span>
                      </div>
                      <span className="status-pill status-muted">{reminderLevelLabel(language, customer.highestReminderLevel)}</span>
                      <strong>{money(customer.openAmount)}</strong>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="col-xl-4">
          <div className="card ops-card dunning-side-card">
            <div className="card-body">
              <div className="form-panel-header">
                <span className="form-panel-icon"><MailPlus size={18} /></span>
                <div>
                  <h4 className="card-title mb-1">{t(language, "recordReminder")}</h4>
                  <p className="text-muted mb-0">{selected ? selected.invoiceNumber : t(language, "selectInvoiceForReminder")}</p>
                </div>
              </div>
              {!selected ? <div className="text-muted">{t(language, "selectInvoiceForReminder")}</div> : (
                <>
                  <div className="dunning-preview">
                    <div><span>{t(language, "customer")}</span><strong>{selected.customerName}</strong></div>
                    <div><span>{t(language, "dueDate")}</span><strong>{selected.dueDate}</strong></div>
                    <div><span>{t(language, "openAmount")}</span><strong>{money(selected.openAmount)}</strong></div>
                    <div><span>{t(language, "nextReminder")}</span><strong>{reminderLevelLabel(language, Math.min((selected.reminderLevel || 0) + 1, 4))}</strong></div>
                  </div>
                  <div className="form-group">
                    <label>{t(language, "reminderNote")}</label>
                    <textarea className="form-control" rows="5" value={note} onChange={(event) => setNote(event.target.value)} placeholder={t(language, "reminderNotePlaceholder")} />
                  </div>
                  <button className="btn btn-danger w-100" type="button" disabled={!canWrite || saving} onClick={recordReminder}>
                    <MailPlus size={16} /> {saving ? `${t(language, "loading")}...` : t(language, "recordReminder")}
                  </button>
                  {canWrite && (
                    <button className="btn btn-outline-primary w-100 mt-2" type="button" onClick={() => setEmailOpen(true)}>
                      <Mail size={16} /> {t(language, "sendEmail")}
                    </button>
                  )}
                  {selected.lastReminderAtUtc && <p className="text-muted mb-0 mt-3">{t(language, "lastReminder")}: {new Date(selected.lastReminderAtUtc).toLocaleString()}</p>}
                </>
              )}
            </div>
          </div>
        </div>
      </div>
      <SendEmailModal
        open={emailOpen}
        language={language}
        documentType="dunning"
        defaultSubject={selected ? `Mahnung – Rechnung ${selected.invoiceNumber}` : ""}
        defaultBody={selected ? `Sehr geehrte Damen und Herren,\n\nhiermit erinnern wir Sie an die offene Zahlung von ${money(selected.openAmount)} für Rechnung ${selected.invoiceNumber}.\n\nMit freundlichen Grüßen\nEasyMitt` : ""}
        onSend={async (body) => {
          if (!selected) return;
          await emailApi.sendDunning(selected.invoiceDraftId, body);
          setMessage(["success", t(language, "emailSent")]);
          setEmailOpen(false);
        }}
        onClose={() => setEmailOpen(false)}
      />
    </>
  );
}

function SummaryTile({ icon: Icon, label, value }) {
  return <div className="summary-tile"><span><Icon size={14} /> {label}</span><strong>{value}</strong></div>;
}

function money(value) {
  return `${Number(value || 0).toFixed(2)} EUR`;
}

function reminderLevelLabel(language, level = 0) {
  const key = {
    0: "noReminder",
    1: "friendlyReminder",
    2: "firstMahnung",
    3: "secondMahnung",
    4: "finalNotice"
  }[level] || "finalNotice";
  return t(language, key);
}
