import { useEffect, useState } from "react";
import { Camera, Download, Eye, FileSpreadsheet, FileText, Plus, Search } from "lucide-react";
import { Link } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { datevApi } from "../api/datev.js";
import { invoicesApi } from "../api/invoices.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";
import { getDocument } from "../utils/invoice.js";

export default function Drafts() {
  const { language } = useAuth();
  const [drafts, setDrafts] = useState([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  async function load(search = query, selectedStatus = status) {
    setLoading(true);
    setError("");
    try {
      setDrafts(await invoicesApi.listDrafts(search, selectedStatus));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Load failed");
    } finally {
      setLoading(false);
    }
  }

  async function exportDatev() {
    setError("");
    try {
      await datevApi.exportInvoices(status);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Export failed");
    }
  }

  useEffect(() => {
    let alive = true;
    invoicesApi.listDrafts()
      .then((records) => alive && setDrafts(records))
      .catch((err) => alive && setError(err instanceof ApiError ? err.message : "Load failed"))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  return (
    <>
      <PageTitle
        title={t(language, "invoices")}
        action={
          <div className="page-action-group">
            <Link className="btn btn-secondary" to={`/datev/preview?type=invoices${status ? `&status=${encodeURIComponent(status)}` : ""}`}><FileSpreadsheet size={16} /> {t(language, "datevPreview")}</Link>
            <button className="btn btn-secondary" type="button" onClick={exportDatev}><Download size={16} /> {t(language, "datevCsv")}</button>
            <Link className="btn btn-secondary" to="/invoices/raw"><Camera size={16} /> {t(language, "import")}</Link>
            <Link className="btn btn-primary" to="/invoices/new"><Plus size={16} /> {t(language, "newInvoice")}</Link>
          </div>
        }
      />
      {error && <div className="alert alert-danger">{error}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "totalDrafts")} value={drafts.length} />
        <SummaryTile label={t(language, "issued")} value={countStatus(drafts, ["Issued", "Sent", "Overdue"])} />
        <SummaryTile label={t(language, "paid")} value={countStatus(drafts, ["Paid"])} />
        <SummaryTile label={t(language, "overdue")} value={countStatus(drafts, ["Overdue"])} />
      </div>
      <div className="card ops-card">
        <div className="card-body">
          <div className="ops-toolbar">
            <div>
              <h4 className="card-title mb-1">{t(language, "invoiceDirectory")}</h4>
              <p className="text-muted mb-0">{t(language, "invoiceDirectoryHint")}</p>
            </div>
            <div className="filter-control invoice-filter-control">
              <span className="filter-icon"><Search size={16} /></span>
              <input className="form-control" value={query} onChange={(e) => setQuery(e.target.value)} placeholder={t(language, "search")} />
              <select className="form-control" value={status} onChange={(e) => setStatus(e.target.value)}>
                <option value="">{t(language, "allStatuses")}</option>
                {["Draft", "Issued", "Sent", "Paid", "Overdue", "Cancelled"].map((item) => <option key={item} value={item}>{statusLabel(language, item)}</option>)}
              </select>
              <button className="btn btn-secondary" onClick={() => load()}>{t(language, "search")}</button>
            </div>
          </div>
          {loading ? <div className="text-muted">{t(language, "loading")}...</div> : drafts.length === 0 ? <div className="text-muted">{t(language, "noDrafts")}</div> : (
            <div className="table-responsive">
              <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                <thead>
                  <tr>
                    <th>{t(language, "invoiceNumber")}</th>
                    <th>{t(language, "buyer")}</th>
                    <th>{t(language, "issueDate")}</th>
                    <th>{t(language, "status")}</th>
                    <th>{t(language, "total")}</th>
                    <th className="text-right">{t(language, "actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {drafts.map((draft) => {
                    const document = getDocument(draft);
                    return (
                      <tr key={draft.id}>
                        <td><div className="entity-cell"><span className="entity-avatar avatar-product"><FileText size={18} /></span><span><strong>{document?.core?.["BT-1"] || "-"}</strong><small>{draft.createdAtUtc ? new Date(draft.createdAtUtc).toLocaleString() : "-"}</small></span></div></td>
                        <td>{document?.buyer?.["BT-26"] || "-"}</td>
                        <td>{document?.core?.["BT-2"] || "-"}</td>
                        <td><span className={`status-pill ${statusClass(draft.status)}`}>{statusLabel(language, draft.status)}</span></td>
                        <td>{Number(document?.core?.["BT-112"] || 0).toFixed(2)} {document?.core?.["BT-5"] || "EUR"}</td>
                        <td className="text-right"><Link className="btn btn-icon btn-soft-primary" to={`/invoices/${draft.id}`} title={t(language, "open")}><Eye size={15} /></Link></td>
                      </tr>
                    );
                  })}
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

function countStatus(drafts, statuses) {
  return drafts.filter((draft) => statuses.includes(draft.status || "Draft")).length;
}

function statusLabel(language, status = "Draft") {
  return t(language, `invoiceStatus${status}`);
}

function statusClass(status = "Draft") {
  return {
    Draft: "status-muted",
    Issued: "status-info",
    Sent: "status-info",
    PartiallyPaid: "status-info",
    Paid: "status-ready",
    Overdue: "status-risk",
    Cancelled: "status-muted"
  }[status] || "status-muted";
}
