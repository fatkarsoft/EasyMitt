import { useState } from "react";
import { useNavigate } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import FormField from "../components/FormField.js";
import { invoicesApi } from "../api/invoices.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const initial = {
  merchantOrSellerHint: "",
  buyerHint: "",
  ibanOrPaymentHint: "",
  sellerVatIdHint: "",
  buyerVatIdHint: "",
  buyerReferenceHint: "",
  totalAmount: "",
  currencyHint: "EUR",
  issueDateHint: new Date().toISOString().slice(0, 10),
  lineHints: [{ description: "", amount: "", vatRatePercent: 19 }]
};

export default function RawImport() {
  const { language, canWrite } = useAuth();
  const navigate = useNavigate();
  const [payload, setPayload] = useState(initial);
  const [document, setDocument] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit() {
    setLoading(true);
    setError("");
    try {
      const body = {
        ...payload,
        totalAmount: payload.totalAmount === "" ? null : Number(payload.totalAmount),
        lineHints: payload.lineHints.map((line) => ({
          description: line.description || null,
          amount: line.amount === "" ? null : Number(line.amount),
          vatRatePercent: line.vatRatePercent === "" ? null : Number(line.vatRatePercent)
        }))
      };
      const result = await invoicesApi.ingestRaw(body);
      setDocument(result.document || result);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Import failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <PageTitle title={t(language, "rawImport")} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {error && <div className="alert alert-danger">{error}</div>}
      <div className="card">
        <div className="card-body">
          <div className="row">
            {[
              ["merchantOrSellerHint", "sellerHint"],
              ["buyerHint", "buyerHint"],
              ["ibanOrPaymentHint", "paymentHint"],
              ["sellerVatIdHint", "sellerVatHint"],
              ["buyerVatIdHint", "buyerVatHint"],
              ["buyerReferenceHint", "buyerReferenceHint"],
              ["currencyHint", "currencyHint"],
              ["issueDateHint", "issueDateHint"],
              ["totalAmount", "totalAmount"]
            ].map(([key, labelKey]) => (
              <div className="col-lg-4" key={key}>
                <FormField label={t(language, labelKey)}>
                  <input className="form-control" type={key === "issueDateHint" ? "date" : key === "totalAmount" ? "number" : "text"} value={payload[key] || ""} onChange={(e) => setPayload({ ...payload, [key]: e.target.value })} />
                </FormField>
              </div>
            ))}
          </div>
          <div className="d-flex justify-content-between align-items-center mt-2 mb-3">
            <h5 className="mb-0">{t(language, "lineHints")}</h5>
            <button className="btn btn-sm btn-outline-primary" disabled={!canWrite} onClick={() => setPayload({ ...payload, lineHints: [...payload.lineHints, { description: "", amount: "", vatRatePercent: 19 }] })}>{t(language, "addLine")}</button>
          </div>
          {payload.lineHints.map((line, index) => (
            <div className="invoice-line" key={index}>
              <div className="row">
                <div className="col-lg-6"><FormField label={t(language, "description")}><input className="form-control" value={line.description} onChange={(e) => changeLine(payload, setPayload, index, { description: e.target.value })} /></FormField></div>
                <div className="col-lg-3"><FormField label={t(language, "amount")}><input className="form-control" type="number" value={line.amount} onChange={(e) => changeLine(payload, setPayload, index, { amount: e.target.value })} /></FormField></div>
                <div className="col-lg-2"><FormField label={t(language, "vatPercent")}><input className="form-control" type="number" value={line.vatRatePercent} onChange={(e) => changeLine(payload, setPayload, index, { vatRatePercent: e.target.value })} /></FormField></div>
                <div className="col-lg-1 d-flex align-items-end"><button className="btn btn-outline-danger btn-block mb-3" aria-label={t(language, "removeLine")} disabled={!canWrite || payload.lineHints.length === 1} onClick={() => setPayload({ ...payload, lineHints: payload.lineHints.filter((_, i) => i !== index) })}>×</button></div>
              </div>
            </div>
          ))}
          <button className="btn btn-primary" disabled={!canWrite || loading} onClick={submit}>{loading ? `${t(language, "loading")}...` : t(language, "import")}</button>
        </div>
      </div>
      {document && (
        <div className="card">
          <div className="card-body">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h4 className="card-title mb-0">{t(language, "document")}</h4>
              <button className="btn btn-success" onClick={() => navigate("/invoices/new", { state: { document } })}>{t(language, "useAsInvoice")}</button>
            </div>
            <pre className="json-preview">{JSON.stringify(document, null, 2)}</pre>
          </div>
        </div>
      )}
    </>
  );
}

function changeLine(payload, setPayload, index, patch) {
  const lineHints = [...payload.lineHints];
  lineHints[index] = { ...lineHints[index], ...patch };
  setPayload({ ...payload, lineHints });
}
