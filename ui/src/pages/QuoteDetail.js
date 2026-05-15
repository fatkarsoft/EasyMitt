import { useEffect, useState } from "react";
import { ArrowLeft, CheckCircle2, FileText, Send, ThumbsDown } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { quotesApi } from "../api/quotes.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";
import { quoteStatusClass, quoteStatusLabel } from "./Quotes.js";

export default function QuoteDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { language, canWrite } = useAuth();
  const [quote, setQuote] = useState(null);
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState("");

  useEffect(() => {
    quotesApi.get(id)
      .then(setQuote)
      .catch((err) => setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]));
  }, [id]);

  async function transition(key, action) {
    setLoading(key);
    setMessage(null);
    try {
      const updated = await action(id);
      setQuote(updated);
      setMessage(["success", t(language, "quoteStatusUpdated")]);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    } finally {
      setLoading("");
    }
  }

  async function convert() {
    setLoading("convert");
    setMessage(null);
    try {
      const result = await quotesApi.convertToInvoice(id);
      const invoiceDraftId = result.invoiceDraftId;
      if (invoiceDraftId) navigate(`/invoices/${invoiceDraftId}`);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    } finally {
      setLoading("");
    }
  }

  const document = quote?.document;

  return (
    <>
      <PageTitle title={t(language, "quoteDetail")} action={<Link className="btn btn-secondary" to="/quotes"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      {!quote || !document ? <div className="card"><div className="card-body text-muted">{t(language, "loading")}...</div></div> : (
        <>
          <div className="row">
            <Info label={t(language, "quoteNumber")} value={quote.quoteNumber} />
            <Info label={t(language, "status")} value={quoteStatusLabel(language, quote.status)} />
            <Info label={t(language, "validUntil")} value={new Date(quote.validUntilUtc).toLocaleDateString()} />
            <Info label={t(language, "total")} value={`${Number(quote.totalAmount || 0).toFixed(2)} ${document.core["BT-5"] || "EUR"}`} />
          </div>
          <div className="card ops-card">
            <div className="card-body">
              <div className="invoice-lifecycle-panel mb-4">
                <div>
                  <span className={`status-pill ${quoteStatusClass(quote.status)}`}>{quoteStatusLabel(language, quote.status)}</span>
                  <p className="text-muted mb-0 mt-2">{t(language, "quoteLifecycleHint")}</p>
                </div>
                <div className="page-action-group">
                  {quote.status === "Draft" && <button className="btn btn-outline-primary" disabled={!canWrite || !!loading} onClick={() => transition("send", quotesApi.send)}><Send size={16} /> {t(language, "markSent")}</button>}
                  {["Draft", "Sent"].includes(quote.status) && <button className="btn btn-outline-success" disabled={!canWrite || !!loading} onClick={() => transition("accept", quotesApi.accept)}><CheckCircle2 size={16} /> {t(language, "markAccepted")}</button>}
                  {["Draft", "Sent"].includes(quote.status) && <button className="btn btn-outline-danger" disabled={!canWrite || !!loading} onClick={() => transition("decline", quotesApi.decline)}><ThumbsDown size={16} /> {t(language, "markDeclined")}</button>}
                  {["Draft", "Sent", "Accepted"].includes(quote.status) && <button className="btn btn-primary" disabled={!canWrite || !!loading} onClick={convert}><FileText size={16} /> {t(language, "convertToInvoice")}</button>}
                  {quote.convertedInvoiceDraftId && <Link className="btn btn-secondary" to={`/invoices/${quote.convertedInvoiceDraftId}`}>{t(language, "openInvoice")}</Link>}
                </div>
              </div>
              <div className="row">
                <Info label={t(language, "buyer")} value={document.buyer["BT-26"] || "-"} small />
                <Info label={t(language, "issueDate")} value={document.core["BT-2"] || "-"} small />
                <Info label={t(language, "buyerReferenceBt")} value={document.core["BT-10"] || "-"} small />
                <Info label={t(language, "total")} value={`${document.core["BT-112"]} ${document.core["BT-5"]}`} small />
              </div>
              <pre className="json-preview">{JSON.stringify(document, null, 2)}</pre>
            </div>
          </div>
        </>
      )}
    </>
  );
}

function Info({ label, value, small }) {
  return (
    <div className={small ? "col-md-3" : "col-xl-3 col-md-6"}>
      <div className="card mini-stat">
        <div className="card-body">
          <div className="mini-stat-label">{label}</div>
          <h5 className="mt-2 mb-0 text-truncate">{value}</h5>
        </div>
      </div>
    </div>
  );
}
