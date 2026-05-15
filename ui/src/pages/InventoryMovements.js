import { useEffect, useState } from "react";
import { ArrowUpDown, Boxes, Plus, Search } from "lucide-react";
import { Link } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { productsApi } from "../api/products.js";
import { inventoryApi } from "../api/inventory.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

export default function InventoryMovements() {
  const { language, canWrite } = useAuth();
  const [products, setProducts] = useState([]);
  const [movements, setMovements] = useState([]);
  const [selectedProduct, setSelectedProduct] = useState("");
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);

  async function load(productId = selectedProduct) {
    setLoading(true);
    try {
      const [productList, movementList] = await Promise.all([
        productsApi.list(),
        inventoryApi.movements(productId)
      ]);
      setProducts(productList);
      setMovements(movementList);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let alive = true;
    Promise.all([productsApi.list(), inventoryApi.movements()])
      .then(([productList, movementList]) => {
        if (!alive) return;
        setProducts(productList);
        setMovements(movementList);
      })
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  const stockedProducts = products.filter((item) => item.type === "Product");
  const totalStock = stockedProducts.reduce((sum, item) => sum + Number(item.currentStock || 0), 0);
  const inbound = movements.filter((item) => Number(item.quantityDelta) > 0).length;
  const outbound = movements.filter((item) => Number(item.quantityDelta) < 0).length;

  return (
    <>
      <PageTitle title={t(language, "inventory")} action={<Link className="btn btn-primary" to="/inventory/new"><Plus size={16} /> {t(language, "addMovement")}</Link>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "stockedProducts")} value={stockedProducts.length} />
        <SummaryTile label={t(language, "totalStock")} value={formatNumber(totalStock)} />
        <SummaryTile label={t(language, "inboundMovements")} value={inbound} />
        <SummaryTile label={t(language, "outboundMovements")} value={outbound} />
      </div>
      <div className="row">
        <div className="col-12">
          <div className="card ops-card">
            <div className="card-body">
              <div className="ops-toolbar">
                <div>
                  <h4 className="card-title mb-1">{t(language, "stockMovementDirectory")}</h4>
                  <p className="text-muted mb-0">{t(language, "stockMovementDirectoryHint")}</p>
                </div>
                <div className="filter-control">
                  <span className="filter-icon"><Search size={16} /></span>
                  <select className="form-control" value={selectedProduct} onChange={(e) => setSelectedProduct(e.target.value)}>
                    <option value="">{t(language, "allProducts")}</option>
                    {stockedProducts.map((item) => <option key={item.id} value={item.id}>{item.sku} · {item.name}</option>)}
                  </select>
                  <button className="btn btn-secondary" onClick={() => load()}>{t(language, "search")}</button>
                </div>
              </div>
              <div className="table-responsive">
                <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                  <thead>
                    <tr>
                      <th>{t(language, "product")}</th>
                      <th>{t(language, "movementType")}</th>
                      <th>{t(language, "quantity")}</th>
                      <th>{t(language, "reason")}</th>
                      <th>{t(language, "createdAt")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {loading ? <tr><td colSpan="5">{t(language, "loading")}...</td></tr> : movements.map((item) => (
                      <tr key={item.id}>
                        <td>
                          <div className="entity-cell">
                            <span className="entity-avatar avatar-product"><Boxes size={18} /></span>
                            <span><strong>{item.productName}</strong><small>{item.productSku}</small></span>
                          </div>
                        </td>
                        <td><span className="status-pill status-muted"><ArrowUpDown size={13} /> {item.type}</span></td>
                        <td><span className={Number(item.quantityDelta) < 0 ? "text-danger font-weight-bold" : "text-success font-weight-bold"}>{formatSigned(item.quantityDelta)}</span></td>
                        <td>{item.reason || "-"}</td>
                        <td>{new Date(item.createdAtUtc).toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

function SummaryTile({ label, value }) {
  return <div className="summary-tile"><span>{label}</span><strong>{value}</strong></div>;
}

function formatNumber(value) {
  return Number(value || 0).toLocaleString(undefined, { maximumFractionDigits: 2 });
}

function formatSigned(value) {
  const number = Number(value || 0);
  return `${number > 0 ? "+" : ""}${formatNumber(number)}`;
}
