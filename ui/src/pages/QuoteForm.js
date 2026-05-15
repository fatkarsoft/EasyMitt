import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, FileSignature, PackagePlus, UserCheck } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import FormField from "../components/FormField.js";
import { quotesApi } from "../api/quotes.js";
import { customersApi } from "../api/customers.js";
import { productsApi } from "../api/products.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";
import { emptyInvoice, recalculate } from "../utils/invoice.js";

export default function QuoteForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { language, canWrite } = useAuth();
  const initial = useMemo(() => {
    const draft = emptyInvoice();
    draft.core["BT-1"] = `ANG-${crypto.randomUUID().slice(0, 8)}`;
    return recalculate(draft);
  }, []);
  const [document, setDocument] = useState(initial);
  const [customers, setCustomers] = useState([]);
  const [products, setProducts] = useState([]);
  const [customerId, setCustomerId] = useState("");
  const [productIds, setProductIds] = useState(initial.lines.map(() => ""));
  const [validUntilUtc, setValidUntilUtc] = useState(defaultValidUntil());
  const [errors, setErrors] = useState({});
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(id ? "load" : "");
  const isEdit = !!id;

  function update(next) {
    setDocument(recalculate(next));
  }

  useEffect(() => {
    let alive = true;
    Promise.all([
      customersApi.list(),
      productsApi.list(),
      id ? quotesApi.get(id) : Promise.resolve(null)
    ])
      .then(([customerList, productList, quote]) => {
        if (!alive) return;
        setCustomers(customerList);
        setProducts(productList);
        if (quote) {
          setDocument(recalculate(quote.document));
          setCustomerId(quote.customerId || "");
          setProductIds((quote.productIds || quote.document.lines.map(() => "")).map((item) => item || ""));
          setValidUntilUtc(toDateInput(quote.validUntilUtc));
        }
      })
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(""));
    return () => { alive = false; };
  }, [id]);

  function errorFor(...keys) {
    const normalize = (value) => value.toLowerCase().replace(/[^a-z0-9]/g, "");
    for (const key of keys) {
      if (errors[key]) return errors[key];
      const found = Object.entries(errors).find(([candidate]) => normalize(candidate).endsWith(normalize(key)));
      if (found) return found[1];
    }
    return null;
  }

  async function submit(event) {
    event.preventDefault();
    setLoading("save");
    setErrors({});
    setMessage(null);
    try {
      const payload = { document, customerId, productIds, validUntilUtc };
      const saved = isEdit ? await quotesApi.update(id, payload) : await quotesApi.create(payload);
      navigate(`/quotes/${saved.id}`);
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
      <PageTitle
        title={isEdit ? t(language, "editQuote") : t(language, "newQuote")}
        action={<Link className="btn btn-secondary" to="/quotes"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>}
      />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="invoice-workflow">
        <div className="workflow-step active"><UserCheck size={18} /><span>{t(language, "selectCustomer")}</span></div>
        <div className="workflow-step active"><PackagePlus size={18} /><span>{t(language, "invoiceLines")}</span></div>
        <div className="workflow-step"><FileSignature size={18} /><span>{t(language, "saveQuote")}</span></div>
      </div>
      <form className="card ops-card" onSubmit={submit}>
        <div className="card-body">
          {loading === "load" ? <div className="text-muted">{t(language, "loading")}...</div> : (
            <>
              <div className="section-heading">
                <div>
                  <h4 className="card-title mb-1">{t(language, "quoteSetup")}</h4>
                  <p className="text-muted mb-0">{t(language, "quoteSetupHint")}</p>
                </div>
              </div>
              <div className="invoice-form-grid invoice-setup-grid">
                <Field span="span-3" label={t(language, "selectCustomer")}>
                  <select className="form-control" value={customerId} onChange={(e) => {
                    setCustomerId(e.target.value);
                    const selected = customers.find((item) => item.id === e.target.value);
                    if (selected) applyCustomer(document, update, selected);
                  }}>
                    <option value="">{t(language, "manualEntry")}</option>
                    {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.displayName}</option>)}
                  </select>
                </Field>
                <Field span="span-3" label={t(language, "quoteNumber")} error={errorFor("document.core.BT-1", "BT-1")}>
                  <input className="form-control" value={document.core["BT-1"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-1": e.target.value } })} />
                </Field>
                <Field span="span-3" label={t(language, "issueDateBt")} error={errorFor("document.core.BT-2", "BT-2")}>
                  <input className="form-control" type="date" value={document.core["BT-2"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-2": e.target.value } })} />
                </Field>
                <Field span="span-3" label={t(language, "validUntil")}>
                  <input className="form-control" type="date" value={validUntilUtc} onChange={(e) => setValidUntilUtc(e.target.value)} />
                </Field>
                <Field span="span-6" label={t(language, "buyerReferenceBt")} error={errorFor("document.core.BT-10", "BT-10")}>
                  <input className="form-control" value={document.core["BT-10"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-10": e.target.value } })} />
                </Field>
                <Field span="span-3" label={t(language, "currencyBt")}>
                  <input className="form-control" value="EUR" readOnly />
                </Field>
              </div>
              <QuoteParties language={language} document={document} update={update} errorFor={errorFor} />
              <QuoteLines language={language} canWrite={canWrite} document={document} update={update} products={products} productIds={productIds} setProductIds={setProductIds} errorFor={errorFor} />
              <div className="totals-bar invoice-totals-bar">
                <span>{t(language, "taxTotalBt")}: <strong>{document.core["BT-110"].toFixed(2)}</strong></span>
                <span>{t(language, "grandTotalBt")}: <strong>{document.core["BT-112"].toFixed(2)}</strong></span>
              </div>
              <div className="invoice-action-footer">
                <button className="btn btn-primary" disabled={!canWrite || loading === "save"}>{loading === "save" ? `${t(language, "loading")}...` : t(language, "saveQuote")}</button>
              </div>
            </>
          )}
        </div>
      </form>
    </>
  );
}

function QuoteParties({ language, document, update, errorFor }) {
  return (
    <>
      <div className="section-heading mt-4">
        <div>
          <h4 className="card-title mb-1">{t(language, "germanyEInvoiceData")}</h4>
          <p className="text-muted mb-0">{t(language, "germanyEInvoiceDataHint")}</p>
        </div>
      </div>
      <div className="invoice-form-grid">
        <Field span="span-4" label={t(language, "sellerNameBt")} error={errorFor("document.seller.BT-20", "BT-20")}><input className="form-control" value={document.seller["BT-20"]} onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-20": e.target.value } })} /></Field>
        <Field span="span-4" label={t(language, "sellerVatBt")} error={errorFor("document.seller.BT-22", "BT-22")}><input className="form-control" value={document.seller["BT-22"]} onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-22": e.target.value } })} /></Field>
        <Field span="span-4" label={t(language, "sellerEndpointBt")} error={errorFor("document.seller.BT-34", "BT-34")}><input className="form-control" value={document.seller["BT-34"]} onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-34": e.target.value } })} /></Field>
        <Field span="span-6" label={t(language, "buyerNameBt")} error={errorFor("document.buyer.BT-26", "BT-26")}><input className="form-control" value={document.buyer["BT-26"]} onChange={(e) => update({ ...document, buyer: { ...document.buyer, "BT-26": e.target.value } })} /></Field>
        <Field span="span-6" label={t(language, "buyerEndpointBt")} error={errorFor("document.buyer.BT-48", "BT-48")}><input className="form-control" value={document.buyer["BT-48"] || ""} onChange={(e) => update({ ...document, buyer: { ...document.buyer, "BT-48": e.target.value || null } })} /></Field>
      </div>
    </>
  );
}

function QuoteLines({ language, canWrite, document, update, products, productIds, setProductIds, errorFor }) {
  return (
    <>
      <div className="section-heading mt-3">
        <div>
          <h4 className="card-title mb-1">{t(language, "invoiceLines")}</h4>
          <p className="text-muted mb-0">{t(language, "invoiceLinesHint")}</p>
        </div>
        <button className="btn btn-sm btn-outline-primary" type="button" disabled={!canWrite} onClick={() => { setProductIds([...productIds, ""]); update({ ...document, lines: [...document.lines, { "BT-126": "", "BT-129": 1, "BT-131": 0, "BT-151": 19 }] }); }}>{t(language, "addLine")}</button>
      </div>
      {document.lines.map((line, index) => (
        <div className="invoice-line invoice-line-card" key={index}>
          <div className="invoice-line-grid">
            <Field span="line-product" label={t(language, "selectProduct")}><select className="form-control" value={productIds[index] || ""} onChange={(e) => {
              const nextIds = [...productIds];
              nextIds[index] = e.target.value;
              setProductIds(nextIds);
              const selected = products.find((item) => item.id === e.target.value);
              if (selected) applyProduct(document, update, index, selected);
            }}><option value="">{t(language, "manualEntry")}</option>{products.map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}</select></Field>
            <Field span="line-description" label={t(language, "lineDescriptionBt")} error={errorFor(`document.lines.${index}.BT-126`, "BT-126")}><input className="form-control" value={line["BT-126"]} onChange={(e) => changeLine(document, update, index, { "BT-126": e.target.value })} /></Field>
            <Field span="line-number" label={t(language, "lineQuantityBt")} error={errorFor(`document.lines.${index}.BT-129`, "BT-129")}><input className="form-control" type="number" value={line["BT-129"]} onChange={(e) => changeLine(document, update, index, { "BT-129": Number(e.target.value) })} /></Field>
            <Field span="line-number" label={t(language, "lineNetBt")} error={errorFor(`document.lines.${index}.BT-131`, "BT-131")}><input className="form-control" type="number" step="0.01" value={line["BT-131"]} onChange={(e) => changeLine(document, update, index, { "BT-131": Number(e.target.value) })} /></Field>
            <Field span="line-vat" label={t(language, "lineVatBt")} error={errorFor(`document.lines.${index}.BT-151`, "BT-151")}><select className="form-control" value={line["BT-151"]} onChange={(e) => changeLine(document, update, index, { "BT-151": Number(e.target.value) })}><option value={0}>0%</option><option value={7}>7%</option><option value={19}>19%</option></select></Field>
            <div className="line-remove"><button type="button" className="btn btn-outline-danger btn-block mb-3" disabled={!canWrite || document.lines.length === 1} onClick={() => { setProductIds(productIds.filter((_, i) => i !== index)); update({ ...document, lines: document.lines.filter((_, i) => i !== index) }); }}>×</button></div>
          </div>
        </div>
      ))}
    </>
  );
}

function Field({ span, label, error, children }) {
  return <div className={span}><FormField label={label} error={error}>{children}</FormField></div>;
}

function changeLine(document, update, index, patch) {
  const lines = [...document.lines];
  lines[index] = { ...lines[index], ...patch };
  update({ ...document, lines });
}

function applyCustomer(document, update, customer) {
  update({
    ...document,
    core: { ...document.core, "BT-10": customer.leitwegId || document.core["BT-10"] },
    buyer: { ...document.buyer, "BT-26": customer.displayName || "", "BT-48": customer.vatId || null }
  });
}

function applyProduct(document, update, index, product) {
  changeLine(document, update, index, {
    "BT-126": product.description || product.name,
    "BT-131": Number(product.netPrice || 0),
    "BT-151": Number(product.vatRatePercent || 19)
  });
}

function defaultValidUntil() {
  const date = new Date();
  date.setDate(date.getDate() + 14);
  return date.toISOString().slice(0, 10);
}

function toDateInput(value) {
  return value ? new Date(value).toISOString().slice(0, 10) : defaultValidUntil();
}
