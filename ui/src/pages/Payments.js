import { Banknote, FileUp, Link2, Search } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import PageTitle from "../components/PageTitle.js";
import { ApiError } from "../api/client.js";
import { paymentsApi } from "../api/payments.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const emptyForm = {
  bookingDate: new Date().toISOString().slice(0, 10),
  description: "",
  counterpartyName: "",
  counterpartyIban: "",
  amount: "",
  currencyCode: "EUR"
};

export default function Payments() {
  const { language, canWrite } = useAuth();
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [form, setForm] = useState(emptyForm);
  const [selected, setSelected] = useState(null);
  const [suggestions, setSuggestions] = useState([]);
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);
  const [matching, setMatching] = useState(false);

  async function load(search = query, selectedStatus = status) {
    setLoading(true);
    setMessage(null);
    try {
      setItems(await paymentsApi.list(search, selectedStatus));
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let alive = true;
    paymentsApi.list()
      .then((data) => alive && setItems(data))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  const summary = useMemo(() => ({
    count: items.length,
    unmatched: items.filter((item) => item.status === "Unmatched").length,
    matched: items.filter((item) => item.status === "Matched").length,
    incoming: items.filter((item) => item.direction === "Incoming").reduce((sum, item) => sum + Number(item.amount || 0), 0)
  }), [items]);

  async function saveTransaction(event) {
    event.preventDefault();
    setMessage(null);
    try {
      await paymentsApi.create({ ...form, amount: Number(form.amount) });
      setForm(emptyForm);
      setMessage(["success", t(language, "paymentSaved")]);
      await load();
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Save failed"]);
    }
  }

  async function importCsv(event) {
    const file = event.target.files?.[0];
    if (!file) return;
    setMessage(null);
    try {
      const imported = await paymentsApi.importCsv(file);
      setMessage(["success", `${t(language, "paymentImportCompleted")} (${imported.length})`]);
      await load();
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Import failed"]);
    } finally {
      event.target.value = "";
    }
  }

  async function showSuggestions(transaction) {
    setSelected(transaction);
    setMatching(true);
    setMessage(null);
    try {
      setSuggestions(await paymentsApi.suggestions(transaction.id));
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Suggestions failed"]);
    } finally {
      setMatching(false);
    }
  }

  async function allocate(suggestion) {
    if (!selected) return;
    setMessage(null);
    const amount = Math.min(Number(selected.unmatchedAmount || 0), Number(suggestion.openAmount || 0));
    try {
      await paymentsApi.allocate({
        bankTransactionId: selected.id,
        invoiceDraftId: suggestion.invoiceDraftId,
        amount
      });
      setMessage(["success", t(language, "paymentAllocated")]);
      setSelected(null);
      setSuggestions([]);
      await load();
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Match failed"]);
    }
  }

  return (
    <>
      <PageTitle
        title={t(language, "payments")}
        action={
          <label className={`btn btn-secondary mb-0 ${!canWrite ? "disabled" : ""}`}>
            <FileUp size={16} /> {t(language, "importBankCsv")}
            <input type="file" accept=".csv,text/csv" hidden disabled={!canWrite} onChange={importCsv} />
          </label>
        }
      />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "bankTransactions")} value={summary.count} />
        <SummaryTile label={t(language, "paymentStatusUnmatched")} value={summary.unmatched} />
        <SummaryTile label={t(language, "paymentStatusMatched")} value={summary.matched} />
        <SummaryTile label={t(language, "cashIn")} value={summary.incoming.toFixed(2)} />
      </div>

      <div className="row">
        <div className="col-xl-8">
          <div className="card ops-card">
            <div className="card-body">
              <div className="ops-toolbar">
                <div>
                  <h4 className="card-title mb-1">{t(language, "bankTransactionDirectory")}</h4>
                  <p className="text-muted mb-0">{t(language, "bankTransactionDirectoryHint")}</p>
                </div>
                <div className="filter-control invoice-filter-control">
                  <span className="filter-icon"><Search size={16} /></span>
                  <input className="form-control" value={query} onChange={(event) => setQuery(event.target.value)} placeholder={t(language, "search")} />
                  <select className="form-control" value={status} onChange={(event) => setStatus(event.target.value)}>
                    <option value="">{t(language, "allStatuses")}</option>
                    {["Unmatched", "PartiallyMatched", "Matched"].map((item) => <option key={item} value={item}>{paymentStatusLabel(language, item)}</option>)}
                  </select>
                  <button className="btn btn-secondary" type="button" onClick={() => load()}>{t(language, "search")}</button>
                </div>
              </div>
              {loading ? <div className="text-muted">{t(language, "loading")}...</div> : items.length === 0 ? <div className="text-muted">{t(language, "noPayments")}</div> : (
                <div className="table-responsive">
                  <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                    <thead><tr><th>{t(language, "bookingDate")}</th><th>{t(language, "description")}</th><th>{t(language, "amount")}</th><th>{t(language, "status")}</th><th className="text-right">{t(language, "actions")}</th></tr></thead>
                    <tbody>
                      {items.map((item) => (
                        <tr key={item.id}>
                          <td>{item.bookingDate}</td>
                          <td><div className="entity-cell"><span className="entity-avatar avatar-product"><Banknote size={18} /></span><span><strong>{item.counterpartyName || item.description}</strong><small>{item.description}</small></span></div></td>
                          <td className={item.amount >= 0 ? "text-success" : "text-danger"}>{Number(item.amount).toFixed(2)} {item.currencyCode}<small className="d-block text-muted">{t(language, "openAmount")}: {Number(item.unmatchedAmount || 0).toFixed(2)}</small></td>
                          <td><span className={`status-pill ${paymentStatusClass(item.status)}`}>{paymentStatusLabel(language, item.status)}</span></td>
                          <td className="text-right">
                            <button className="btn btn-icon btn-soft-primary" type="button" disabled={!canWrite || item.unmatchedAmount <= 0} onClick={() => showSuggestions(item)} title={t(language, "matchPayment")}><Link2 size={15} /></button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
        <div className="col-xl-4">
          <form className="card ops-card" onSubmit={saveTransaction}>
            <div className="card-body">
              <div className="form-panel-header">
                <span className="form-panel-icon"><Banknote size={18} /></span>
                <div><h4 className="card-title mb-1">{t(language, "newBankTransaction")}</h4><p className="text-muted mb-0">{t(language, "newBankTransactionHint")}</p></div>
              </div>
              <Field label={t(language, "bookingDate")}><input className="form-control" type="date" value={form.bookingDate} onChange={(event) => setForm({ ...form, bookingDate: event.target.value })} /></Field>
              <Field label={t(language, "description")}><input className="form-control" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></Field>
              <Field label={t(language, "counterpartyName")}><input className="form-control" value={form.counterpartyName} onChange={(event) => setForm({ ...form, counterpartyName: event.target.value })} /></Field>
              <Field label={t(language, "counterpartyIban")}><input className="form-control" value={form.counterpartyIban} onChange={(event) => setForm({ ...form, counterpartyIban: event.target.value })} /></Field>
              <div className="balanced-form-grid">
                <Field span="span-6" label={t(language, "amount")}><input className="form-control" type="number" step="0.01" value={form.amount} onChange={(event) => setForm({ ...form, amount: event.target.value })} /></Field>
                <Field span="span-6" label={t(language, "currency")}><input className="form-control" value={form.currencyCode} onChange={(event) => setForm({ ...form, currencyCode: event.target.value })} /></Field>
              </div>
              <button className="btn btn-primary w-100" type="submit" disabled={!canWrite}>{t(language, "save")}</button>
            </div>
          </form>
          <div className="card ops-card">
            <div className="card-body">
              <h4 className="card-title mb-1">{t(language, "matchSuggestions")}</h4>
              <p className="text-muted">{selected ? selected.description : t(language, "selectTransactionForMatch")}</p>
              {matching ? <div className="text-muted">{t(language, "loading")}...</div> : suggestions.length === 0 ? <div className="text-muted">{t(language, "noSuggestions")}</div> : (
                <div className="datev-mapping-list">
                  {suggestions.map((suggestion) => (
                    <div className="payment-suggestion" key={suggestion.invoiceDraftId}>
                      <div>
                        <strong>{suggestion.invoiceNumber}</strong>
                        <small>{suggestion.buyerName} · {t(language, "openAmount")}: {Number(suggestion.openAmount).toFixed(2)}</small>
                        <span className="status-pill status-info">{suggestion.score}%</span>
                      </div>
                      <button className="btn btn-sm btn-primary" type="button" disabled={!canWrite} onClick={() => allocate(suggestion)}>{t(language, "match")}</button>
                    </div>
                  ))}
                </div>
              )}
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

function Field({ label, span = "", children }) {
  return <div className={`balanced-field ${span}`}><div className="form-group"><label>{label}</label>{children}</div></div>;
}

function paymentStatusLabel(language, status = "Unmatched") {
  return t(language, `paymentStatus${status}`);
}

function paymentStatusClass(status = "Unmatched") {
  return { Unmatched: "status-risk", PartiallyMatched: "status-info", Matched: "status-ready" }[status] || "status-muted";
}
