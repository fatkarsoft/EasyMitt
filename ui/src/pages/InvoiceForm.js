import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import FormField from "../components/FormField.js";
import { invoicesApi } from "../api/invoices.js";
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
  const [errors, setErrors] = useState({});
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState("");

  function update(next) {
    setDocument(recalculate(next));
  }

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
        const saved = await invoicesApi.saveDraft(document);
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
      <PageTitle title={t(language, "newInvoice")} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="card">
        <div className="card-body">
          <h4 className="card-title mb-4">{t(language, "en16931Section")}</h4>
          <div className="row">
            <Field col="col-lg-3" label={t(language, "invoiceNumberBt")} error={errorFor("core.BT-1", "BT-1")}>
              <input className="form-control" value={document.core["BT-1"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-1": e.target.value } })} />
            </Field>
            <Field col="col-lg-3" label={t(language, "issueDateBt")} error={errorFor("core.BT-2", "BT-2")}>
              <input className="form-control" type="date" value={document.core["BT-2"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-2": e.target.value } })} />
            </Field>
            <Field col="col-lg-2" label={t(language, "currencyBt")}>
              <input className="form-control" value="EUR" readOnly />
            </Field>
            <Field col="col-lg-4" label={t(language, "buyerReferenceBt")} error={errorFor("core.BT-10", "BT-10")}>
              <input className="form-control" value={document.core["BT-10"]} onChange={(e) => update({ ...document, core: { ...document.core, "BT-10": e.target.value } })} />
            </Field>
            <Field col="col-lg-4" label={t(language, "sellerNameBt")} error={errorFor("seller.BT-20", "BT-20")}>
              <input className="form-control" value={document.seller["BT-20"]} onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-20": e.target.value } })} />
            </Field>
            <Field col="col-lg-4" label={t(language, "sellerVatBt")} error={errorFor("seller.BT-22", "BT-22")}>
              <input className="form-control" value={document.seller["BT-22"]} onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-22": e.target.value } })} />
            </Field>
            <Field col="col-lg-4" label={t(language, "sellerEndpointBt")} error={errorFor("seller.BT-34", "BT-34")}>
              <input className="form-control" value={document.seller["BT-34"]} onChange={(e) => update({ ...document, seller: { ...document.seller, "BT-34": e.target.value } })} />
            </Field>
            <Field col="col-lg-6" label={t(language, "buyerNameBt")} error={errorFor("buyer.BT-26", "BT-26")}>
              <input className="form-control" value={document.buyer["BT-26"]} onChange={(e) => update({ ...document, buyer: { ...document.buyer, "BT-26": e.target.value } })} />
            </Field>
            <Field col="col-lg-6" label={t(language, "buyerEndpointBt")} error={errorFor("buyer.BT-48", "BT-48")}>
              <input className="form-control" value={document.buyer["BT-48"] || ""} onChange={(e) => update({ ...document, buyer: { ...document.buyer, "BT-48": e.target.value || null } })} />
            </Field>
          </div>
          <div className="d-flex justify-content-between align-items-center mt-2 mb-3">
            <h5 className="mb-0">{t(language, "invoiceLines")}</h5>
            <button className="btn btn-sm btn-outline-primary" disabled={!canWrite} onClick={() => update({ ...document, lines: [...document.lines, { "BT-126": "", "BT-129": 1, "BT-131": 0, "BT-151": 19 }] })}>{t(language, "addLine")}</button>
          </div>
          {document.lines.map((line, index) => (
            <div className="invoice-line" key={index}>
              <div className="row">
                <Field col="col-lg-5" label={t(language, "lineDescriptionBt")} error={errorFor(`lines.${index}.BT-126`, "BT-126")}>
                  <input className="form-control" value={line["BT-126"]} onChange={(e) => changeLine(document, update, index, { "BT-126": e.target.value })} />
                </Field>
                <Field col="col-lg-2" label={t(language, "lineQuantityBt")} error={errorFor(`lines.${index}.BT-129`, "BT-129")}>
                  <input className="form-control" type="number" value={line["BT-129"]} onChange={(e) => changeLine(document, update, index, { "BT-129": Number(e.target.value) })} />
                </Field>
                <Field col="col-lg-2" label={t(language, "lineNetBt")} error={errorFor(`lines.${index}.BT-131`, "BT-131")}>
                  <input className="form-control" type="number" step="0.01" value={line["BT-131"]} onChange={(e) => changeLine(document, update, index, { "BT-131": Number(e.target.value) })} />
                </Field>
                <Field col="col-lg-2" label={t(language, "lineVatBt")} error={errorFor(`lines.${index}.BT-151`, "BT-151")}>
                  <select className="form-control" value={line["BT-151"]} onChange={(e) => changeLine(document, update, index, { "BT-151": Number(e.target.value) })}>
                    <option value={0}>0%</option><option value={7}>7%</option><option value={19}>19%</option>
                  </select>
                </Field>
                <div className="col-lg-1 d-flex align-items-end">
                  <button className="btn btn-outline-danger btn-block mb-3" aria-label={t(language, "removeLine")} disabled={!canWrite || document.lines.length === 1} onClick={() => update({ ...document, lines: document.lines.filter((_, i) => i !== index) })}>×</button>
                </div>
              </div>
            </div>
          ))}
          <div className="totals-bar">
            <span>{t(language, "taxTotalBt")}: <strong>{document.core["BT-110"].toFixed(2)}</strong></span>
            <span>{t(language, "grandTotalBt")}: <strong>{document.core["BT-112"].toFixed(2)}</strong></span>
          </div>
          <div className="button-items mt-4">
            <button className="btn btn-secondary" disabled={loading === "validate"} onClick={() => run("validate")}>{loading === "validate" ? `${t(language, "loading")}...` : t(language, "validate")}</button>
            <button className="btn btn-primary" disabled={!canWrite || loading === "save"} onClick={() => run("save")}>{loading === "save" ? `${t(language, "loading")}...` : t(language, "saveDraft")}</button>
          </div>
        </div>
      </div>
    </>
  );
}

function Field({ col, label, error, children }) {
  return <div className={col}><FormField label={label} error={error}>{children}</FormField></div>;
}

function changeLine(document, update, index, patch) {
  const lines = [...document.lines];
  lines[index] = { ...lines[index], ...patch };
  update({ ...document, lines });
}
