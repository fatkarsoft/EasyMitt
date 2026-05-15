import { useEffect, useState } from "react";
import { Download, Edit2, FileSpreadsheet, ReceiptText, Search } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { datevApi } from "../api/datev.js";
import { expensesApi } from "../api/expenses.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

export default function Expenses() {
  const { language } = useAuth();
  const navigate = useNavigate();
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);

  async function load(search = query, selectedStatus = status) {
    setLoading(true);
    try {
      setItems(await expensesApi.list(search, selectedStatus));
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  async function exportDatev() {
    try {
      await datevApi.exportExpenses(status);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Export failed"]);
    }
  }

  useEffect(() => {
    let alive = true;
    expensesApi.list()
      .then((data) => alive && setItems(data))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  return (
    <>
      <PageTitle
        title={t(language, "expenses")}
        action={
          <div className="page-action-group">
            <Link className="btn btn-secondary" to={`/datev/preview?type=expenses${status ? `&status=${encodeURIComponent(status)}` : ""}`}><FileSpreadsheet size={16} /> {t(language, "datevPreview")}</Link>
            <button className="btn btn-secondary" type="button" onClick={exportDatev}><Download size={16} /> {t(language, "datevCsv")}</button>
            <Link className="btn btn-primary" to="/expenses/new">{t(language, "newExpense")}</Link>
          </div>
        }
      />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "totalExpenses")} value={items.length} />
        <SummaryTile label={t(language, "expenseStatusInbox")} value={countStatus(items, "Inbox")} />
        <SummaryTile label={t(language, "expenseStatusBooked")} value={countStatus(items, "Booked")} />
        <SummaryTile label={t(language, "total")} value={sumTotal(items).toFixed(2)} />
      </div>
      <div className="card ops-card">
        <div className="card-body">
          <div className="ops-toolbar">
            <div>
              <h4 className="card-title mb-1">{t(language, "expenseDirectory")}</h4>
              <p className="text-muted mb-0">{t(language, "expenseDirectoryHint")}</p>
            </div>
            <div className="filter-control invoice-filter-control">
              <span className="filter-icon"><Search size={16} /></span>
              <input className="form-control" value={query} onChange={(e) => setQuery(e.target.value)} placeholder={t(language, "search")} />
              <select className="form-control" value={status} onChange={(e) => setStatus(e.target.value)}>
                <option value="">{t(language, "allStatuses")}</option>
                {["Inbox", "Booked", "Archived"].map((item) => <option key={item} value={item}>{expenseStatusLabel(language, item)}</option>)}
              </select>
              <button className="btn btn-secondary" onClick={() => load()}>{t(language, "search")}</button>
            </div>
          </div>
          {loading ? <div className="text-muted">{t(language, "loading")}...</div> : items.length === 0 ? <div className="text-muted">{t(language, "noExpenses")}</div> : (
            <div className="table-responsive">
              <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                <thead><tr><th>{t(language, "vendor")}</th><th>{t(language, "category")}</th><th>{t(language, "issueDate")}</th><th>{t(language, "status")}</th><th>{t(language, "total")}</th><th className="text-right">{t(language, "actions")}</th></tr></thead>
                <tbody>
                  {items.map((item) => (
                    <tr key={item.id}>
                      <td><div className="entity-cell"><span className="entity-avatar avatar-product"><ReceiptText size={18} /></span><span><strong>{item.vendorName}</strong><small>{item.documentNumber || "-"}</small></span></div></td>
                      <td>{item.category}</td>
                      <td>{item.issueDate}</td>
                      <td><span className={`status-pill ${expenseStatusClass(item.status)}`}>{expenseStatusLabel(language, item.status)}</span></td>
                      <td>{Number(item.totalAmount).toFixed(2)} {item.currencyCode}</td>
                      <td className="text-right"><button className="btn btn-icon btn-soft-primary" onClick={() => navigate(`/expenses/${item.id}/edit`)} title={t(language, "editExpense")}><Edit2 size={15} /></button></td>
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

function countStatus(items, status) {
  return items.filter((item) => item.status === status).length;
}

function sumTotal(items) {
  return items.reduce((sum, item) => sum + Number(item.totalAmount || 0), 0);
}

export function expenseStatusLabel(language, status = "Inbox") {
  return t(language, `expenseStatus${status}`);
}

export function expenseStatusClass(status = "Inbox") {
  return { Inbox: "status-info", Booked: "status-ready", Archived: "status-muted" }[status] || "status-muted";
}
