import { useEffect, useState } from "react";
import { ArrowLeft, ArrowUpDown } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import FormField from "../components/FormField.js";
import { productsApi } from "../api/products.js";
import { inventoryApi } from "../api/inventory.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const emptyMovement = {
  productId: "",
  type: "Adjustment",
  quantityDelta: 0,
  reason: ""
};

export default function InventoryMovementForm() {
  const navigate = useNavigate();
  const { language, canWrite } = useAuth();
  const [products, setProducts] = useState([]);
  const [form, setForm] = useState(emptyMovement);
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let alive = true;
    productsApi.list()
      .then((data) => alive && setProducts(data.filter((item) => item.type === "Product")))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  async function submit(event) {
    event.preventDefault();
    setMessage(null);
    try {
      await inventoryApi.createMovement({
        ...form,
        quantityDelta: Number(form.quantityDelta)
      });
      navigate("/inventory");
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    }
  }

  return (
    <>
      <PageTitle
        title={t(language, "newStockMovement")}
        action={<Link className="btn btn-secondary" to="/inventory"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>}
      />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="row">
        <div className="col-12">
          <form className="card ops-card" onSubmit={submit}>
            <div className="card-body">
              <div className="form-panel-header">
                <span className="form-panel-icon"><ArrowUpDown size={18} /></span>
                <div>
                  <h4 className="card-title mb-1">{t(language, "newStockMovement")}</h4>
                  <p className="text-muted mb-0">{t(language, "stockMovementFormHint")}</p>
                </div>
              </div>
              {loading ? <div className="text-muted">{t(language, "loading")}...</div> : (
                <>
                  <div className="balanced-form-grid">
                    <Field span="span-6" label={t(language, "selectProduct")}>
                      <select className="form-control" value={form.productId} onChange={(e) => set(form, setForm, "productId", e.target.value)}>
                        <option value="">{t(language, "selectProduct")}</option>
                        {products.map((item) => <option key={item.id} value={item.id}>{item.sku} · {item.name}</option>)}
                      </select>
                    </Field>
                    <Field span="span-3" label={t(language, "movementType")}>
                      <select className="form-control" value={form.type} onChange={(e) => set(form, setForm, "type", e.target.value)}>
                        <option value="OpeningBalance">{t(language, "openingBalance")}</option>
                        <option value="Purchase">{t(language, "purchase")}</option>
                        <option value="Adjustment">{t(language, "adjustment")}</option>
                      </select>
                    </Field>
                    <Field span="span-3" label={t(language, "quantity")}>
                      <input className="form-control" type="number" step="0.001" value={form.quantityDelta} onChange={(e) => set(form, setForm, "quantityDelta", e.target.value)} />
                    </Field>
                    <Field span="span-12" label={t(language, "reason")}>
                      <textarea className="form-control" rows="4" value={form.reason} onChange={(e) => set(form, setForm, "reason", e.target.value)} />
                    </Field>
                  </div>
                  <div className="button-items mt-3">
                    <button className="btn btn-primary" disabled={!canWrite}>{t(language, "save")}</button>
                    <Link className="btn btn-secondary" to="/inventory">{t(language, "cancel")}</Link>
                  </div>
                </>
              )}
            </div>
          </form>
        </div>
      </div>
    </>
  );
}

function Field({ label, span = "span-6", children }) {
  return <div className={`balanced-field ${span}`}><FormField label={label}>{children}</FormField></div>;
}

function set(form, setForm, key, value) {
  setForm({ ...form, [key]: value });
}
