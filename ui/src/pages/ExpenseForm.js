import { useEffect, useState } from "react";
import { ArrowLeft, ReceiptText, UploadCloud } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import FormField from "../components/FormField.js";
import AiSuggestionPill from "../components/AiSuggestionPill.js";
import { expensesApi } from "../api/expenses.js";
import { aiApi } from "../api/ai.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const emptyExpense = {
  vendorName: "",
  documentNumber: "",
  issueDate: new Date().toISOString().slice(0, 10),
  category: "General",
  netAmount: 0,
  taxAmount: 0,
  totalAmount: 0,
  currencyCode: "EUR",
  datevCreditorAccount: "",
  notes: ""
};

export default function ExpenseForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { language, canWrite } = useAuth();
  const [form, setForm] = useState(emptyExpense);
  const [message, setMessage] = useState(null);
  const [scanFile, setScanFile] = useState(null);
  const [loading, setLoading] = useState(id ? "load" : "");
  const [categorySuggestion, setCategorySuggestion] = useState(null);
  const [datevSuggestion, setDatevSuggestion] = useState(null);
  const isEdit = !!id;

  useEffect(() => {
    if (!id) return;
    let alive = true;
    expensesApi.get(id)
      .then((data) => alive && setForm({ ...emptyExpense, ...data }))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(""));
    return () => { alive = false; };
  }, [id]);

  useEffect(() => {
    if (!id) return;
    let alive = true;
    aiApi.suggestDatev("Expense", id)
      .then((data) => { if (alive) setDatevSuggestion(data ? { ...data, status: "Pending" } : null); })
      .catch(() => {});
    return () => { alive = false; };
  }, [id]);

  async function refreshCategorySuggestion(nextForm) {
    try {
      const lines = nextForm.notes || "";
      const result = await aiApi.suggestCategory({
        vendorName: nextForm.vendorName || "",
        lineDescriptions: lines,
        totalAmount: Number(nextForm.totalAmount || 0) || null,
        currencyCode: nextForm.currencyCode || "EUR"
      });
      if (!result || !result.category) return;
      if (nextForm.category && nextForm.category !== "General" && nextForm.category === result.category) {
        setCategorySuggestion(null);
        return;
      }
      setCategorySuggestion({ ...result, status: "Pending" });
    } catch {
      // best-effort only
    }
  }

  useEffect(() => {
    if (!form.vendorName) return;
    const handle = setTimeout(() => refreshCategorySuggestion(form), 250);
    return () => clearTimeout(handle);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [form.vendorName, form.notes, form.totalAmount]);

  async function acceptCategory() {
    if (!categorySuggestion) return;
    setForm((current) => ({ ...current, category: categorySuggestion.category }));
    try {
      await aiApi.log({
        suggestionType: "ExpenseCategory",
        targetType: "Expense",
        targetId: id || null,
        payload: categorySuggestion
      }, "Accepted");
    } catch { /* ignore */ }
    setCategorySuggestion({ ...categorySuggestion, status: "Accepted" });
  }

  async function rejectCategory() {
    if (!categorySuggestion) return;
    try {
      await aiApi.log({
        suggestionType: "ExpenseCategory",
        targetType: "Expense",
        targetId: id || null,
        payload: categorySuggestion
      }, "Rejected");
    } catch { /* ignore */ }
    setCategorySuggestion({ ...categorySuggestion, status: "Rejected" });
  }

  async function acceptDatev() {
    if (!datevSuggestion) return;
    setForm((current) => ({ ...current, datevCreditorAccount: datevSuggestion.account }));
    try {
      await aiApi.log({
        suggestionType: "DatevAccount",
        targetType: "Expense",
        targetId: id || null,
        payload: datevSuggestion
      }, "Accepted");
    } catch { /* ignore */ }
    setDatevSuggestion({ ...datevSuggestion, status: "Accepted" });
  }

  async function rejectDatev() {
    if (!datevSuggestion) return;
    try {
      await aiApi.log({
        suggestionType: "DatevAccount",
        targetType: "Expense",
        targetId: id || null,
        payload: datevSuggestion
      }, "Rejected");
    } catch { /* ignore */ }
    setDatevSuggestion({ ...datevSuggestion, status: "Rejected" });
  }

  async function scan() {
    if (!scanFile) return;
    setLoading("scan");
    setMessage(null);
    try {
      const result = await expensesApi.scan(scanFile);
      setForm({ ...emptyExpense, ...result.expense });
      if (result.suggestion && result.suggestion.confidence > 0) {
        setCategorySuggestion({ ...result.suggestion, status: "Pending" });
      }
      setMessage(["success", t(language, "expenseScanMapped")]);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Scan failed"]);
    } finally {
      setLoading("");
    }
  }

  async function submit(event) {
    event.preventDefault();
    setLoading("save");
    setMessage(null);
    try {
      const payload = normalize(form);
      const saved = isEdit ? await expensesApi.update(id, payload) : await expensesApi.create(payload);
      navigate("/expenses");
      return saved;
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    } finally {
      setLoading("");
    }
  }

  async function updateStatus(action) {
    setLoading(action);
    try {
      await (action === "book" ? expensesApi.book(id) : expensesApi.archive(id));
      navigate("/expenses");
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    } finally {
      setLoading("");
    }
  }

  return (
    <>
      <PageTitle title={isEdit ? t(language, "editExpense") : t(language, "newExpense")} action={<Link className="btn btn-secondary" to="/expenses"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      {!isEdit && (
        <div className="card scan-import-card">
          <div className="card-body">
            <div className="row align-items-center">
              <div className="col-lg-7">
                <div className="d-flex align-items-start">
                  <div className="scan-import-icon"><ReceiptText size={22} /></div>
                  <div><h4 className="card-title mb-2">{t(language, "expenseScanTitle")}</h4><p className="text-muted mb-0">{t(language, "expenseScanHint")}</p></div>
                </div>
              </div>
              <div className="col-lg-5 mt-3 mt-lg-0">
                <label className="scan-upload-zone">
                  <UploadCloud size={20} /><span>{t(language, "chooseScanFile")}</span>
                  <input accept="image/jpeg,image/png,image/webp" capture="environment" disabled={!canWrite} onChange={(event) => setScanFile(event.target.files?.[0] || null)} type="file" />
                </label>
                {scanFile && <div className="scan-file-name">{t(language, "selectedFile")}: <strong>{scanFile.name}</strong></div>}
                <button className="btn btn-primary btn-block mt-3" disabled={!canWrite || !scanFile || loading === "scan"} onClick={scan}>{loading === "scan" ? `${t(language, "loading")}...` : t(language, "analyzeScan")}</button>
              </div>
            </div>
          </div>
        </div>
      )}
      <form className="card ops-card" onSubmit={submit}>
        <div className="card-body">
          <div className="form-panel-header">
            <span className="form-panel-icon"><ReceiptText size={18} /></span>
            <div><h4 className="card-title mb-1">{isEdit ? t(language, "editExpense") : t(language, "newExpense")}</h4><p className="text-muted mb-0">{t(language, "expenseFormHint")}</p></div>
          </div>
          {loading === "load" ? <div className="text-muted">{t(language, "loading")}...</div> : (
            <>
              <div className="balanced-form-grid">
                <Field span="span-6" label={t(language, "vendor")}><input className="form-control" value={form.vendorName || ""} onChange={(e) => set(form, setForm, "vendorName", e.target.value)} /></Field>
                <Field span="span-3" label={t(language, "documentNumber")}><input className="form-control" value={form.documentNumber || ""} onChange={(e) => set(form, setForm, "documentNumber", e.target.value)} /></Field>
                <Field span="span-3" label={t(language, "issueDate")}><input className="form-control" type="date" value={form.issueDate || ""} onChange={(e) => set(form, setForm, "issueDate", e.target.value)} /></Field>
                <div className="balanced-field span-3">
                  <FormField label={t(language, "category")}>
                    <input className="form-control" value={form.category || ""} onChange={(e) => set(form, setForm, "category", e.target.value)} />
                  </FormField>
                  {categorySuggestion && (
                    <AiSuggestionPill
                      language={language}
                      label={t(language, "aiSuggestedCategory")}
                      value={t(language, `aiCategory${categorySuggestion.category}`) || categorySuggestion.category}
                      confidence={categorySuggestion.confidence}
                      rationale={t(language, `aiCategoryRationale_${categorySuggestion.rationale}`) || categorySuggestion.rationale}
                      status={categorySuggestion.status}
                      onAccept={canWrite ? acceptCategory : null}
                      onReject={canWrite ? rejectCategory : null}
                      disabled={!canWrite}
                    />
                  )}
                </div>
                <Field span="span-3" label={t(language, "netAmount")}><input className="form-control" type="number" step="0.01" value={form.netAmount} onChange={(e) => set(form, setForm, "netAmount", e.target.value)} /></Field>
                <Field span="span-3" label={t(language, "taxAmount")}><input className="form-control" type="number" step="0.01" value={form.taxAmount} onChange={(e) => set(form, setForm, "taxAmount", e.target.value)} /></Field>
                <Field span="span-3" label={t(language, "totalAmount")}><input className="form-control" type="number" step="0.01" value={form.totalAmount} onChange={(e) => set(form, setForm, "totalAmount", e.target.value)} /></Field>
                <Field span="span-3" label={t(language, "currencyHint")}><input className="form-control" value={form.currencyCode || "EUR"} onChange={(e) => set(form, setForm, "currencyCode", e.target.value)} /></Field>
                <div className="balanced-field span-3">
                  <FormField label={t(language, "datevCreditorAccount")}>
                    <input className="form-control" value={form.datevCreditorAccount || ""} onChange={(e) => set(form, setForm, "datevCreditorAccount", e.target.value)} placeholder="70001" />
                  </FormField>
                  {datevSuggestion && (
                    <AiSuggestionPill
                      language={language}
                      label={t(language, "aiSuggestedDatevAccount")}
                      value={datevSuggestion.account}
                      confidence={datevSuggestion.confidence}
                      rationale={datevSuggestion.rationale}
                      status={datevSuggestion.status}
                      onAccept={canWrite ? acceptDatev : null}
                      onReject={canWrite ? rejectDatev : null}
                      disabled={!canWrite}
                    />
                  )}
                </div>
                <Field span="span-12" label={t(language, "notes")}><textarea className="form-control" rows="4" value={form.notes || ""} onChange={(e) => set(form, setForm, "notes", e.target.value)} /></Field>
              </div>
              <div className="invoice-action-footer">
                {isEdit && <button className="btn btn-outline-success" type="button" disabled={!canWrite || !!loading} onClick={() => updateStatus("book")}>{t(language, "bookExpense")}</button>}
                {isEdit && <button className="btn btn-outline-secondary" type="button" disabled={!canWrite || !!loading} onClick={() => updateStatus("archive")}>{t(language, "archive")}</button>}
                <button className="btn btn-primary" disabled={!canWrite || loading === "save"}>{loading === "save" ? `${t(language, "loading")}...` : t(language, "save")}</button>
              </div>
            </>
          )}
        </div>
      </form>
    </>
  );
}

function Field({ label, span = "span-6", children }) {
  return <div className={`balanced-field ${span}`}><FormField label={label}>{children}</FormField></div>;
}

function set(form, setForm, key, value) {
  setForm({ ...form, [key]: value });
}

function normalize(form) {
  return {
    ...form,
    netAmount: Number(form.netAmount || 0),
    taxAmount: Number(form.taxAmount || 0),
    totalAmount: Number(form.totalAmount || 0)
  };
}
