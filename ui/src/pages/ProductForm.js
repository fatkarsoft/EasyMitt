import { useEffect, useState } from "react";
import { ArrowLeft, Boxes } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import FormField from "../components/FormField.js";
import { productsApi } from "../api/products.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const emptyProduct = {
  type: "Product",
  sku: "",
  name: "",
  description: "",
  unit: "pcs",
  netPrice: 0,
  vatRatePercent: 19,
  currentStock: 0,
  minimumStock: 0,
  isActive: true
};

export default function ProductForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { language, canWrite } = useAuth();
  const [form, setForm] = useState(emptyProduct);
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(!!id);
  const isEdit = !!id;
  const isService = form.type === "Service";

  useEffect(() => {
    if (!id) return;
    let alive = true;
    productsApi.get(id)
      .then((data) => alive && setForm({ ...emptyProduct, ...data }))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, [id]);

  async function submit(event) {
    event.preventDefault();
    setMessage(null);
    try {
      const payload = normalizeProduct(form);
      if (isEdit) {
        await productsApi.update(id, payload);
      } else {
        await productsApi.create(payload);
      }
      navigate("/products");
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    }
  }

  return (
    <>
      <PageTitle
        title={isEdit ? t(language, "editProduct") : t(language, "newProduct")}
        action={<Link className="btn btn-secondary" to="/products"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>}
      />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="row">
        <div className="col-12">
          <form className="card ops-card" onSubmit={submit}>
            <div className="card-body">
              <div className="form-panel-header">
                <span className="form-panel-icon"><Boxes size={18} /></span>
                <div>
                  <h4 className="card-title mb-1">{isEdit ? t(language, "editProduct") : t(language, "newProduct")}</h4>
                  <p className="text-muted mb-0">{t(language, "productFormHint")}</p>
                </div>
              </div>
              {loading ? <div className="text-muted">{t(language, "loading")}...</div> : (
                <>
                  <div className="balanced-form-grid">
                    <Field span="span-3" label={t(language, "type")}>
                      <select className="form-control" value={form.type} onChange={(e) => setForm(switchProductType(form, e.target.value))}>
                        <option value="Product">{t(language, "productOption")}</option>
                        <option value="Service">{t(language, "serviceOption")}</option>
                      </select>
                    </Field>
                    <Field span="span-3" label="SKU"><input className="form-control" value={form.sku} onChange={(e) => set(form, setForm, "sku", e.target.value)} /></Field>
                    <Field span="span-6" label={t(language, "name")}><input className="form-control" value={form.name} onChange={(e) => set(form, setForm, "name", e.target.value)} /></Field>
                    <Field span="span-12" label={t(language, "description")}><textarea className="form-control" rows="3" value={form.description || ""} onChange={(e) => set(form, setForm, "description", e.target.value)} /></Field>
                    <Field span="span-3" label={t(language, "unit")}><input className="form-control" value={form.unit} onChange={(e) => set(form, setForm, "unit", e.target.value)} /></Field>
                    <Field span="span-3" label={t(language, "price")}><input className="form-control" type="number" step="0.01" value={form.netPrice} onChange={(e) => set(form, setForm, "netPrice", Number(e.target.value))} /></Field>
                    <Field span="span-3" label={t(language, "vatPercent")}>
                      <select className="form-control" value={form.vatRatePercent} onChange={(e) => set(form, setForm, "vatRatePercent", Number(e.target.value))}>
                        <option value={0}>0%</option>
                        <option value={7}>7%</option>
                        <option value={19}>19%</option>
                      </select>
                    </Field>
                    <Field span="span-3" label={t(language, "status")}>
                      <select className="form-control" value={form.isActive ? "true" : "false"} onChange={(e) => set(form, setForm, "isActive", e.target.value === "true")}>
                        <option value="true">{t(language, "active")}</option>
                        <option value="false">{t(language, "inactive")}</option>
                      </select>
                    </Field>
                    {!isService && (
                      <>
                        <Field span="span-4" label={t(language, "stock")}><input className="form-control" type="number" step="0.001" disabled={isEdit} value={form.currentStock} onChange={(e) => set(form, setForm, "currentStock", Number(e.target.value))} /></Field>
                        <Field span="span-4" label={t(language, "minStock")}><input className="form-control" type="number" step="0.001" value={form.minimumStock} onChange={(e) => set(form, setForm, "minimumStock", Number(e.target.value))} /></Field>
                        <div className="balanced-field span-4 stock-note">
                          <span>{t(language, "stockManagedHint")}</span>
                        </div>
                      </>
                    )}
                  </div>
                  <div className="button-items mt-3">
                    <button className="btn btn-primary" disabled={!canWrite}>{t(language, "save")}</button>
                    <Link className="btn btn-secondary" to="/products">{t(language, "cancel")}</Link>
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

function switchProductType(form, type) {
  if (type === "Service") {
    return {
      ...form,
      type,
      currentStock: 0,
      minimumStock: 0
    };
  }

  return {
    ...form,
    type: "Product"
  };
}

function normalizeProduct(form) {
  return switchProductType(form, form.type);
}
