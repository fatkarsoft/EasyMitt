import { AlertTriangle, Download, FileSpreadsheet, RefreshCw } from "lucide-react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";
import PageTitle from "../components/PageTitle.js";
import ConfirmDialog from "../components/ConfirmDialog.js";
import { datevApi } from "../api/datev.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

export default function DatevPreview() {
  const { language } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const type = params.get("type") === "expenses" ? "expenses" : "invoices";
  const status = params.get("status") || "";
  const from = params.get("from") || "";
  const to = params.get("to") || "";
  const [filters, setFilters] = useState({ from, to });
  const [preview, setPreview] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [confirmForce, setConfirmForce] = useState(false);

  const backTarget = "/datev";
  const title = type === "expenses" ? t(language, "datevExpensePreview") : t(language, "datevInvoicePreview");

  useEffect(() => {
    let alive = true;
    async function loadPreview() {
      setLoading(true);
      setError("");
      try {
        const data = type === "expenses"
          ? await datevApi.previewExpenses({ status, from, to })
          : await datevApi.previewInvoices({ status, from, to });
        if (alive) setPreview(data);
      } catch (err) {
        if (alive) setError(err instanceof ApiError ? err.message : "Preview failed");
      } finally {
        if (alive) setLoading(false);
      }
    }

    loadPreview();
    return () => { alive = false; };
  }, [from, status, to, type]);

  const rows = useMemo(() => preview?.rows || [], [preview]);

  async function downloadCsv(force = false) {
    setError("");
    try {
      if (type === "expenses") {
        await datevApi.exportExpenses({ status, from, to, force });
      } else {
        await datevApi.exportInvoices({ status, from, to, force });
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Export failed");
    } finally {
      setConfirmForce(false);
    }
  }

  function applyFilters(event) {
    event.preventDefault();
    const query = new URLSearchParams();
    query.set("type", type);
    if (status) query.set("status", status);
    if (filters.from) query.set("from", filters.from);
    if (filters.to) query.set("to", filters.to);
    navigate(`/datev/preview?${query.toString()}`);
  }

  return (
    <>
      <PageTitle
        title={title}
        action={
          <div className="page-action-group">
            <Link className="btn btn-secondary" to={backTarget}>{t(language, "backToList")}</Link>
            <button className="btn btn-outline-primary" type="button" onClick={() => setConfirmForce(true)}><RefreshCw size={16} /> {t(language, "datevForceExport")}</button>
            <button className="btn btn-primary" type="button" onClick={() => downloadCsv()}><Download size={16} /> {t(language, "datevCsv")}</button>
          </div>
        }
      />
      <ConfirmDialog
        open={confirmForce}
        eyebrow={t(language, "areYouSure")}
        title={t(language, "datevForceExportTitle")}
        message={t(language, "datevForceExportMessage")}
        confirmLabel={t(language, "datevForceExport")}
        cancelLabel={t(language, "cancel")}
        variant="danger"
        onConfirm={() => downloadCsv(true)}
        onCancel={() => setConfirmForce(false)}
      />
      {error && <div className="alert alert-danger">{error}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "datevExportFormat")} value={formatLabel(language, preview?.exportFormat)} />
        <SummaryTile label={t(language, "period")} value={periodLabel(from, to)} />
        <SummaryTile label={t(language, "datevRows")} value={rows.length} />
        <SummaryTile label={t(language, "datevReady")} value={preview?.readyCount || 0} />
      </div>
      <form className="card ops-card datev-period-card" onSubmit={applyFilters}>
        <div className="card-body">
          <div className="balanced-form-grid">
            <Field span="span-4" label={t(language, "fromDate")}><input className="form-control" type="date" value={filters.from} onChange={(e) => setFilters({ ...filters, from: e.target.value })} /></Field>
            <Field span="span-4" label={t(language, "toDate")}><input className="form-control" type="date" value={filters.to} onChange={(e) => setFilters({ ...filters, to: e.target.value })} /></Field>
            <div className="balanced-field span-4 datev-period-actions"><button className="btn btn-primary" type="submit">{t(language, "refresh")}</button></div>
          </div>
        </div>
      </form>
      <div className="card ops-card">
        <div className="card-body">
          <div className="ops-toolbar">
            <div>
              <h4 className="card-title mb-1">{t(language, "datevPreview")}</h4>
              <p className="text-muted mb-0">{t(language, "datevPreviewHint")}</p>
            </div>
          </div>
          {loading ? <div className="text-muted">{t(language, "loading")}...</div> : rows.length === 0 ? <div className="text-muted">{t(language, "noDatevRows")}</div> : (
            <div className="table-responsive">
              <table className="table table-centered table-nowrap table-hover ops-table datev-preview-table mb-0">
                <thead>
                  <tr>
                    <th>{t(language, "documentNumber")}</th>
                    <th>{t(language, "issueDate")}</th>
                    <th>{t(language, "description")}</th>
                    <th>{t(language, "account")}</th>
                    <th>{t(language, "offsetAccount")}</th>
                    <th>{t(language, "amount")}</th>
                    <th>{t(language, "taxAmount")}</th>
                    <th>{t(language, "vatPercent")}</th>
                    <th>{t(language, "datevTaxKey")}</th>
                    <th>{t(language, "status")}</th>
                    <th>{t(language, "datevWarnings")}</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row, index) => (
                    <tr key={`${row.documentNumber}-${index}`} className={row.warnings?.length ? "datev-row-warning" : ""}>
                      <td><div className="entity-cell"><span className="entity-avatar avatar-product"><FileSpreadsheet size={18} /></span><span><strong>{row.documentNumber || "-"}</strong><small>{row.source}</small></span></div></td>
                      <td>{row.documentDate}</td>
                      <td>{row.bookingText || "-"}</td>
                      <td><span className="datev-account">{row.account}</span></td>
                      <td><span className="datev-account">{row.offsetAccount}</span></td>
                      <td>{Number(row.amount || 0).toFixed(2)} {row.currencyCode}</td>
                      <td>{Number(row.taxAmount || 0).toFixed(2)} {row.currencyCode}</td>
                      <td>{Number(row.vatRate || 0).toFixed(0)}%</td>
                      <td>{row.taxKey || "-"}</td>
                      <td>{row.status}</td>
                      <td>{row.warnings?.length ? <WarningList warnings={row.warnings} language={language} /> : <span className="status-pill status-ready">{t(language, "datevReady")}</span>}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </>
  );
}

function SummaryTile({ label, value }) {
  return <div className="summary-tile"><span>{label}</span><strong>{value}</strong></div>;
}

function Field({ label, span = "span-6", children }) {
  return <div className={`balanced-field ${span}`}><div className="form-group"><label>{label}</label>{children}</div></div>;
}

function WarningList({ warnings, language }) {
  return (
    <div className="datev-warning-list">
      <AlertTriangle size={15} />
      <span>{warnings.map((warning) => t(language, `datevWarning_${warning}`)).join(", ")}</span>
    </div>
  );
}

function formatLabel(language, format = "BasicCsv") {
  return format === "DatevExtf" ? t(language, "datevFormatExtf") : t(language, "datevFormatBasicCsv");
}

function periodLabel(from, to) {
  if (!from && !to) return "-";
  return `${from || "..."} / ${to || "..."}`;
}
