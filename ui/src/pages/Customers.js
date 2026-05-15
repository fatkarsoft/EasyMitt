import { useEffect, useState } from "react";
import { Building2, Edit2, Mail, Search, Trash2, UserRound } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import ConfirmDialog from "../components/ConfirmDialog.js";
import { customersApi } from "../api/customers.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

export default function Customers() {
  const { language, canWrite } = useAuth();
  const navigate = useNavigate();
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState("");
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);
  const [pendingArchive, setPendingArchive] = useState(null);

  async function load() {
    setLoading(true);
    try {
      setItems(await customersApi.list(query));
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let alive = true;
    customersApi.list()
      .then((data) => alive && setItems(data))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  async function archive() {
    if (!pendingArchive) return;
    await customersApi.archive(pendingArchive.id);
    setPendingArchive(null);
    await load();
  }

  return (
    <>
      <PageTitle title={t(language, "customers")} action={<Link className="btn btn-primary" to="/customers/new">{t(language, "newCustomer")}</Link>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "totalCustomers")} value={items.length} />
        <SummaryTile label="B2B" value={items.filter((item) => item.type === "Business").length} />
        <SummaryTile label="B2C" value={items.filter((item) => item.type === "Consumer").length} />
        <SummaryTile label={t(language, "leitwegId")} value={items.filter((item) => item.leitwegId).length} />
      </div>
      <div className="row">
        <div className="col-12">
          <div className="card ops-card">
            <div className="card-body">
              <div className="ops-toolbar">
                <div>
                  <h4 className="card-title mb-1">{t(language, "customerDirectory")}</h4>
                  <p className="text-muted mb-0">{t(language, "customerDirectoryHint")}</p>
                </div>
                <div className="filter-control">
                  <span className="filter-icon"><Search size={16} /></span>
                  <input className="form-control" value={query} onChange={(e) => setQuery(e.target.value)} placeholder={t(language, "search")} />
                  <button className="btn btn-secondary" onClick={load}>{t(language, "search")}</button>
                </div>
              </div>
              <div className="table-responsive">
                <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                  <thead>
                    <tr><th>{t(language, "customer")}</th><th>{t(language, "eInvoiceProfile")}</th><th>{t(language, "contact")}</th><th>{t(language, "terms")}</th><th className="text-right">{t(language, "actions")}</th></tr>
                  </thead>
                  <tbody>
                    {loading ? <tr><td colSpan="5">{t(language, "loading")}...</td></tr> : items.map((item) => (
                      <tr key={item.id}>
                        <td>
                          <div className="entity-cell">
                            <span className={`entity-avatar ${item.type === "Business" ? "avatar-business" : "avatar-consumer"}`}>{item.type === "Business" ? <Building2 size={18} /> : <UserRound size={18} />}</span>
                            <span><strong>{item.displayName}</strong><small>{item.city || "DE"} · {item.type === "Business" ? "B2B" : "B2C"}</small></span>
                          </div>
                        </td>
                        <td><span className={item.leitwegId ? "status-pill status-ready" : "status-pill status-muted"}>{item.leitwegId ? "Leitweg ready" : "Standard"}</span><div className="text-muted small mt-1">{item.vatId || item.taxNumber || "-"}</div></td>
                        <td>{item.email ? <span className="icon-text"><Mail size={14} /> {item.email}</span> : <span className="text-muted">-</span>}</td>
                        <td>{item.paymentTermsDays} {t(language, "days")}</td>
                        <td className="text-right">
                          <div className="table-action-group justify-content-end">
                          <button className="btn btn-icon btn-soft-primary" onClick={() => navigate(`/customers/${item.id}/edit`)} title={t(language, "editCustomer")}><Edit2 size={15} /></button>
                          <button className="btn btn-icon btn-soft-danger" disabled={!canWrite} onClick={() => setPendingArchive(item)} title={t(language, "delete")}><Trash2 size={15} /></button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
      <ConfirmDialog
        open={!!pendingArchive}
        title={t(language, "archiveCustomerTitle")}
        eyebrow={t(language, "areYouSure")}
        message={pendingArchive ? `${pendingArchive.displayName} - ${t(language, "confirmArchiveCustomer")}` : ""}
        confirmLabel={t(language, "delete")}
        cancelLabel={t(language, "cancel")}
        onConfirm={archive}
        onCancel={() => setPendingArchive(null)}
      />
    </>
  );
}

function SummaryTile({ label, value }) {
  return <div className="summary-tile"><span>{label}</span><strong>{value}</strong></div>;
}
