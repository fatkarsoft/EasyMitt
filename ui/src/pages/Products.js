import { useEffect, useState } from "react";
import { AlertTriangle, Boxes, Edit2, PackageCheck, Search, Trash2 } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import ConfirmDialog from "../components/ConfirmDialog.js";
import { productsApi } from "../api/products.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

export default function Products() {
  const { language, canWrite } = useAuth();
  const navigate = useNavigate();
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState("");
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);
  const [pendingDelete, setPendingDelete] = useState(null);

  async function load(search = query) {
    setLoading(true);
    try {
      setItems(await productsApi.list(search));
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let alive = true;
    productsApi.list()
      .then((data) => alive && setItems(data))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  async function removeProduct() {
    if (!pendingDelete) return;
    await productsApi.archive(pendingDelete.id);
    setPendingDelete(null);
    await load();
  }

  const productCount = items.filter((item) => item.type === "Product").length;
  const serviceCount = items.filter((item) => item.type === "Service").length;
  const lowStockCount = items.filter((item) => item.type === "Product" && Number(item.currentStock) <= Number(item.minimumStock)).length;

  return (
    <>
      <PageTitle title={t(language, "products")} action={<Link className="btn btn-primary" to="/products/new">{t(language, "newProduct")}</Link>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "totalProducts")} value={items.length} />
        <SummaryTile label={t(language, "product")} value={productCount} />
        <SummaryTile label={t(language, "service")} value={serviceCount} />
        <SummaryTile label={t(language, "lowStock")} value={lowStockCount} />
      </div>
      <div className="row">
        <div className="col-12">
          <div className="card ops-card">
            <div className="card-body">
              <div className="ops-toolbar">
                <div>
                  <h4 className="card-title mb-1">{t(language, "productDirectory")}</h4>
                  <p className="text-muted mb-0">{t(language, "productDirectoryHint")}</p>
                </div>
                <div className="filter-control">
                  <span className="filter-icon"><Search size={16} /></span>
                  <input className="form-control" value={query} onChange={(e) => setQuery(e.target.value)} placeholder={t(language, "search")} />
                  <button className="btn btn-secondary" onClick={() => load()}>{t(language, "search")}</button>
                </div>
              </div>
              <div className="table-responsive">
                <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                  <thead>
                    <tr>
                      <th>{t(language, "product")}</th>
                      <th>{t(language, "type")}</th>
                      <th>{t(language, "price")}</th>
                      <th>{t(language, "vatPercent")}</th>
                      <th>{t(language, "stockStatus")}</th>
                      <th className="text-right">{t(language, "actions")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {loading ? <tr><td colSpan="6">{t(language, "loading")}...</td></tr> : items.map((item) => (
                      <tr key={item.id}>
                        <td>
                          <div className="entity-cell">
                            <span className="entity-avatar avatar-product"><Boxes size={18} /></span>
                            <span>
                              <strong>{item.name}</strong>
                              <small><span className="text-monospace">{item.sku}</span> · {item.unit}</small>
                            </span>
                          </div>
                        </td>
                        <td><span className="status-pill status-muted">{item.type === "Service" ? t(language, "service") : t(language, "product")}</span></td>
                        <td>{Number(item.netPrice).toFixed(2)} EUR</td>
                        <td>{item.vatRatePercent}%</td>
                        <td>
                          {item.type === "Service" ? (
                            <span className="status-pill status-ready"><PackageCheck size={13} /> {t(language, "notStocked")}</span>
                          ) : item.currentStock <= item.minimumStock ? (
                            <span className="status-pill status-risk"><AlertTriangle size={13} /> {item.currentStock}</span>
                          ) : (
                            <span className="status-pill status-ready">{item.currentStock}</span>
                          )}
                          <div className="text-muted small mt-1">{t(language, "minStock")} {item.minimumStock}</div>
                        </td>
                        <td className="text-right">
                          <div className="table-action-group justify-content-end">
                            <button className="btn btn-icon btn-soft-primary" onClick={() => navigate(`/products/${item.id}/edit`)} title={t(language, "editProduct")}><Edit2 size={15} /></button>
                            <button className="btn btn-icon btn-soft-danger" disabled={!canWrite} onClick={() => setPendingDelete(item)} title={t(language, "delete")}><Trash2 size={15} /></button>
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
        open={!!pendingDelete}
        title={t(language, "deleteProductTitle")}
        eyebrow={t(language, "areYouSure")}
        message={pendingDelete ? `${pendingDelete.name} - ${t(language, "confirmDeleteProduct")}` : ""}
        confirmLabel={t(language, "delete")}
        cancelLabel={t(language, "cancel")}
        onConfirm={removeProduct}
        onCancel={() => setPendingDelete(null)}
      />
    </>
  );
}

function SummaryTile({ label, value }) {
  return <div className="summary-tile"><span>{label}</span><strong>{value}</strong></div>;
}
