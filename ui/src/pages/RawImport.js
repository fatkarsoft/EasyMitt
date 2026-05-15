import { useState } from "react";
import { ArrowLeft, Camera, UploadCloud } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
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
  const [scanFile, setScanFile] = useState(null);
  const [scanLoading, setScanLoading] = useState(false);

  async function submitScan() {
    if (!scanFile) return;
    setScanLoading(true);
    setError("");
    try {
      const result = await invoicesApi.ingestScan(scanFile);
      setDocument(result.document || result);
      if (result.raw) {
        setPayload({
          merchantOrSellerHint: result.raw.merchantOrSellerHint || "",
          buyerHint: result.raw.buyerHint || "",
          ibanOrPaymentHint: result.raw.ibanOrPaymentHint || "",
          sellerVatIdHint: result.raw.sellerVatIdHint || "",
          buyerVatIdHint: result.raw.buyerVatIdHint || "",
          buyerReferenceHint: result.raw.buyerReferenceHint || "",
          totalAmount: result.raw.totalAmount ?? "",
          currencyHint: result.raw.currencyHint || "EUR",
          issueDateHint: result.raw.issueDateHint || new Date().toISOString().slice(0, 10),
          lineHints: result.raw.lineHints?.length ? result.raw.lineHints.map((line) => ({
            description: line.description || "",
            amount: line.amount ?? "",
            vatRatePercent: line.vatRatePercent ?? 19
          })) : initial.lineHints
        });
      }
    } catch (err) {
      if (err instanceof ApiError) {
        const scanDetail = err.fieldErrors.scan?.[0];
        setError(scanDetail ? `${err.message} ${scanDetail}` : err.message);
      } else {
        setError("Scan failed");
      }
    } finally {
      setScanLoading(false);
    }
  }

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
      <PageTitle title={t(language, "rawImport")} action={<Link className="btn btn-secondary" to="/invoices"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {error && <div className="alert alert-danger">{error}</div>}
      <div className="card scan-import-card">
        <div className="card-body">
          <div className="row align-items-center">
            <div className="col-lg-7">
              <div className="d-flex align-items-start">
                <div className="scan-import-icon"><Camera size={22} /></div>
                <div>
                  <h4 className="card-title mb-2">{t(language, "scanImportTitle")}</h4>
                  <p className="text-muted mb-2">{t(language, "scanImportSubtitle")}</p>
                  <small className="text-muted">{t(language, "scanImportHint")}</small>
                </div>
              </div>
            </div>
            <div className="col-lg-5 mt-3 mt-lg-0">
              <label className={`scan-upload-zone ${!canWrite ? "disabled" : ""}`}>
                <UploadCloud size={20} />
                <span>{t(language, "chooseScanFile")}</span>
                <input
                  accept="image/jpeg,image/png,image/webp"
                  capture="environment"
                  disabled={!canWrite}
                  onChange={(event) => setScanFile(event.target.files?.[0] || null)}
                  type="file"
                />
              </label>
              {scanFile && <div className="scan-file-name">{t(language, "selectedFile")}: <strong>{scanFile.name}</strong></div>}
              <button className="btn btn-primary btn-block mt-3" disabled={!canWrite || !scanFile || scanLoading} onClick={submitScan}>
                {scanLoading ? `${t(language, "loading")}...` : t(language, "analyzeScan")}
              </button>
            </div>
          </div>
        </div>
      </div>
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
                  <input
                    className="form-control"
                    type={key === "issueDateHint" ? "date" : key === "totalAmount" ? "number" : "text"}
                    value={payload[key] || ""}
                    onBlur={(e) => setPayload({ ...payload, [key]: key === "ibanOrPaymentHint" ? cleanPastedWhitespace(e.target.value) : e.target.value.trim() })}
                    onChange={(e) => setPayload({ ...payload, [key]: e.target.value })}
                  />
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

function cleanPastedWhitespace(value) {
  return value.replace(/\s+/g, " ").trim();
}
