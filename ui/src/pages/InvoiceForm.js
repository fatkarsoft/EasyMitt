import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, FileCheck2, PackagePlus, UserCheck } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import FormField from "../components/FormField.js";
import { invoicesApi } from "../api/invoices.js";
import { customersApi } from "../api/customers.js";
import { productsApi } from "../api/products.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";
import { emptyInvoice, getDraftId, recalculate, rememberDraftId } from "../utils/invoice.js";

export default function InvoiceForm() {
  const { language, canWrite } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const initial = useMemo(() => recalculate(location.state?.document || emptyInvoice()), [location.state]);
  const [document, setDocument] = useState(initial);
  const [customers, setCustomers] = useState([]);
  const [products, setProducts] = useState([]);
  const [customerId, setCustomerId] = useState("");
  const [productIds, setProductIds] = useState(initial.lines.map(() => ""));
  const [errors, setErrors] = useState({});
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState("");

  function update(next) {
    setDocument(recalculate(next));
  }

  useEffect(() => {
    let alive = true;
    Promise.all([customersApi.list(), productsApi.list()])
      .then(([customerList, productList]) => {
        if (!alive) return;
        setCustomers(customerList);
        setProducts(productList);
      })
      .catch(() => {});
    return () => { alive = false; };
  }, []);

  function errorFor(...keys) {
    const normalize = (value) => value.toLowerCase().replace(/[^a-z0-9]/g, "");
    for (const key of keys) {
      if (errors[key]) return errors[key];
      const found = Object.entries(errors).find(([candidate]) => normalize(candidate).endsWith(normalize(key)));
      if (found) return found[1];
    }
    return null;
  }

  async function run(action) {
    setLoading(action);
    setErrors({});
    setMessage(null);
    try {
      if (action === "validate") {
        await invoicesApi.validate(document);
        setMessage(["success", t(language, "valid")]);
      } else {
        const saved = await invoicesApi.saveDraft(document, { customerId, productIds });
        const id = getDraftId(saved);
        if (id) rememberDraftId(id);
        setMessage(["success", t(language, "saved")]);
        if (id) navigate(`/invoices/${id}`);
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setErrors(err.fieldErrors);
        setMessage(["danger", err.message]);
      } else {
        setMessage(["danger", "Request failed"]);
      }
    } finally {
      setLoading("");
    }
  }

  return (
    <>
      <PageTitle title={t(language, "newInvoice")} action={<Link className="btn btn-secondary" to="/invoices"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="invoice-workflow">
        <div className="workflow-step active"><UserCheck size={18} /><span>{t(language, "selectCustomer")}</span></div>
        <div className="workflow-step active"><PackagePlus size={18} /><span>{t(language, "invoiceLines")}</span></div>
        <div className="workflow-step"><FileCheck2 size={18} /><span>{t(language, "validate")}</span></div>
      </div>
      <div className="card ops-card">
        <div className="card-body">
          <div className="section-heading">
            <div>
              <h4 className="card-title mb-1">{t(language, "invoiceSetup")}</h4>
              <p className="text-muted mb-0">{t(language, "invoiceSetupHint")}</p>
            </div>
          </div>
          <div className="invoice-form-grid invoice-setup-grid">
            <Field span="span-3" label={t(language, "selectCustomer")}>
              <select
                className="form-control"
                value={customerId}
                onChange={(e) => {
                  setCustomerId(e.target.value);
                  const selected = customers.find((item) => item.id === e.target.value);
                  if (selected) applyCustomer(document, update, selected);
                }}
              >
                <option value="">{t(language, "manualEntry")}</option>
                {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.displayName}</option>)}
              </select>
            </Field>
            <Field span="span-3" label={t(language, "invoiceNumberBt")} error={errorFor("core.BT-1", "BT-1")}>
              <input className="form-control" value={document.core["BT-1"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-1": e.target.value } })} />
            </Field>
            <Field span="span-3" label={t(language, "issueDateBt")} error={errorFor("core.BT-2", "BT-2")}>
              <input className="form-control" type="date" value={document.core["BT-2"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-2": e.target.value } })} />
            </Field>
            <Field span="span-3" label={t(language, "currencyBt")}>
              <input className="form-control" value="EUR" readOnly />
            </Field>
            <Field span="span-6" label={t(language, "buyerReferenceBt")} error={errorFor("core.BT-10", "BT-10")}>
              <input className="form-control" value={document.core["BT-10"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-10": e.target.value } })} />
            </Field>
          </div>
          <div className="section-heading mt-4">
            <div>
              <h4 className="card-title mb-1">{t(language, "germanyEInvoiceData")}</h4>
              <p className="text-muted mb-0">{t(language, "germanyEInvoiceDataHint")}</p>
            </div>
          </div>
          <div className="invoice-form-grid">
            <Field span="span-4" label={t(language, "sellerNameBt")} error={errorFor("seller.BT-20", "BT-20")}>
              <input className="form-control" value={document.seller["BT-20"]} onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-20": e.target.value } })} />
            </Field>
            <Field span="span-4" label={t(language, "sellerVatBt")} error={errorFor("seller.BT-22", "BT-22")}>
              <input className="form-control" value={document.seller["BT-22"]} onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-22": e.target.value } })} />
            </Field>
            <Field span="span-4" label={t(language, "sellerEndpointBt")} error={errorFor("seller.BT-34", "BT-34")}>
              <input
                className="form-control"
                placeholder={t(language, "ibanPlaceholder")}
                value={document.seller["BT-34"]}
                onBlur={(e) => update({ ...document, seller: { ...document.seller, "BT-34": cleanPastedWhitespace(e.target.value) } })}
                onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-34": e.target.value } })}
              />
            </Field>
            <Field span="span-6" label={t(language, "buyerNameBt")} error={errorFor("buyer.BT-26", "BT-26")}>
              <input className="form-control" value={document.buyer["BT-26"]} onChange={(e) => update({ ...document, buyer: { ...document.buyer, "BT-26": e.target.value } })} />
            </Field>
            <Field span="span-6" label={t(language, "buyerEndpointBt")} error={errorFor("buyer.BT-48", "BT-48")}>
              <input
                className="form-control"
                placeholder={t(language, "vatPlaceholder")}
                value={document.buyer["BT-48"] || ""}
                onChange={(e) => update({ ...document, buyer: { ...document.buyer, "BT-48": e.target.value || null } })}
              />
              {looksLikeIban(document.buyer["BT-48"]) && <small className="text-warning d-block mt-1">{t(language, "buyerVatIbanWarning")}</small>}
            </Field>
          </div>
          <div className="section-heading mt-3">
            <div>
              <h4 className="card-title mb-1">{t(language, "invoiceLines")}</h4>
              <p className="text-muted mb-0">{t(language, "invoiceLinesHint")}</p>
            </div>
            <button className="btn btn-sm btn-outline-primary" disabled={!canWrite} onClick={() => { setProductIds([...productIds, ""]); update({ ...document, lines: [...document.lines, { "BT-126": "", "BT-129": 1, "BT-131": 0, "BT-151": 19 }] }); }}>{t(language, "addLine")}</button>
          </div>
          {document.lines.map((line, index) => (
            <div className="invoice-line invoice-line-card" key={index}>
              <div className="invoice-line-grid">
                <Field span="line-product" label={t(language, "selectProduct")}>
                  <select
                    className="form-control"
                    value={productIds[index] || ""}
                    onChange={(e) => {
                      const nextIds = [...productIds];
                      nextIds[index] = e.target.value;
                      setProductIds(nextIds);
                      const selected = products.find((item) => item.id === e.target.value);
                      if (selected) applyProduct(document, update, index, selected);
                    }}
                  >
                    <option value="">{t(language, "manualEntry")}</option>
                    {products.map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}
                  </select>
                </Field>
                <Field span="line-description" label={t(language, "lineDescriptionBt")} error={errorFor(`lines.${index}.BT-126`, "BT-126")}>
                  <input className="form-control" value={line["BT-126"]} onChange={(e) => changeLine(document, update, index, { "BT-126": e.target.value })} />
                </Field>
                <Field span="line-number" label={t(language, "lineQuantityBt")} error={errorFor(`lines.${index}.BT-129`, "BT-129")}>
                  <input className="form-control" type="number" value={line["BT-129"]} onChange={(e) => changeLine(document, update, index, { "BT-129": Number(e.target.value) })} />
                </Field>
                <Field span="line-number" label={t(language, "lineNetBt")} error={errorFor(`lines.${index}.BT-131`, "BT-131")}>
                  <input className="form-control" type="number" step="0.01" value={line["BT-131"]} onChange={(e) => changeLine(document, update, index, { "BT-131": Number(e.target.value) })} />
                </Field>
                <Field span="line-vat" label={t(language, "lineVatBt")} error={errorFor(`lines.${index}.BT-151`, "BT-151")}>
                  <select className="form-control" value={line["BT-151"]} onChange={(e) => changeLine(document, update, index, { "BT-151": Number(e.target.value) })}>
                    <option value={0}>0%</option><option value={7}>7%</option><option value={19}>19%</option>
                  </select>
                </Field>
                <div className="line-remove">
                  <button className="btn btn-outline-danger btn-block mb-3" aria-label={t(language, "removeLine")} disabled={!canWrite || document.lines.length === 1} onClick={() => { setProductIds(productIds.filter((_, i) => i !== index)); update({ ...document, lines: document.lines.filter((_, i) => i !== index) }); }}>×</button>
                </div>
              </div>
            </div>
          ))}
          <div className="totals-bar invoice-totals-bar">
            <span>{t(language, "taxTotalBt")}: <strong>{document.core["BT-110"].toFixed(2)}</strong></span>
            <span>{t(language, "grandTotalBt")}: <strong>{document.core["BT-112"].toFixed(2)}</strong></span>
          </div>
          <div className="invoice-action-footer">
            <button className="btn btn-secondary" disabled={loading === "validate"} onClick={() => run("validate")}>{loading === "validate" ? `${t(language, "loading")}...` : t(language, "validate")}</button>
            <button className="btn btn-primary" disabled={!canWrite || loading === "save"} onClick={() => run("save")}>{loading === "save" ? `${t(language, "loading")}...` : t(language, "saveDraft")}</button>
          </div>
        </div>
      </div>
    </>
  );
}

function Field({ col, span, label, error, children }) {
  return <div className={span || col}><FormField label={label} error={error}>{children}</FormField></div>;
}

function changeLine(document, update, index, patch) {
  const lines = [...document.lines];
  lines[index] = { ...lines[index], ...patch };
  update({ ...document, lines });
}

function applyCustomer(document, update, customer) {
  update({
    ...document,
    core: {
      ...document.core,
      "BT-10": customer.leitwegId || document.core["BT-10"]
    },
    buyer: {
      ...document.buyer,
      "BT-26": customer.displayName || "",
      "BT-48": customer.vatId || null
    }
  });
}

function applyProduct(document, update, index, product) {
  changeLine(document, update, index, {
    "BT-126": product.description || product.name,
    "BT-131": Number(product.netPrice || 0),
    "BT-151": Number(product.vatRatePercent || 19)
  });
}

function cleanPastedWhitespace(value) {
  return value.replace(/\s+/g, " ").trim();
}

function looksLikeIban(value) {
  return /^[A-Za-z]{2}\s*\d{2}/.test(cleanPastedWhitespace(value || ""));
}
