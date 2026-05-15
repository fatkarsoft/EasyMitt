import { useEffect, useState } from "react";
import { Eye, FileSignature, Plus, Search } from "lucide-react";
import { Link } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { quotesApi } from "../api/quotes.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

export default function Quotes() {
  const { language } = useAuth();
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(true);

  async function load(search = query, selectedStatus = status) {
    setLoading(true);
    try {
      setItems(await quotesApi.list(search, selectedStatus));
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let alive = true;
    quotesApi.list()
      .then((data) => alive && setItems(data))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  return (
    <>
      <PageTitle title={t(language, "quotes")} action={<Link className="btn btn-primary" to="/quotes/new"><Plus size={16} /> {t(language, "newQuote")}</Link>} />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "totalQuotes")} value={items.length} />
        <SummaryTile label={t(language, "quoteStatusSent")} value={countStatus(items, ["Sent"])} />
        <SummaryTile label={t(language, "quoteStatusAccepted")} value={countStatus(items, ["Accepted"])} />
        <SummaryTile label={t(language, "quoteStatusConverted")} value={countStatus(items, ["Converted"])} />
      </div>
      <div className="card ops-card">
        <div className="card-body">
          <div className="ops-toolbar">
            <div>
              <h4 className="card-title mb-1">{t(language, "quoteDirectory")}</h4>
              <p className="text-muted mb-0">{t(language, "quoteDirectoryHint")}</p>
            </div>
            <div className="filter-control invoice-filter-control">
              <span className="filter-icon"><Search size={16} /></span>
              <input className="form-control" value={query} onChange={(e) => setQuery(e.target.value)} placeholder={t(language, "search")} />
              <select className="form-control" value={status} onChange={(e) => setStatus(e.target.value)}>
                <option value="">{t(language, "allStatuses")}</option>
                {["Draft", "Sent", "Accepted", "Declined", "Converted"].map((item) => <option key={item} value={item}>{quoteStatusLabel(language, item)}</option>)}
              </select>
              <button className="btn btn-secondary" onClick={() => load()}>{t(language, "search")}</button>
            </div>
          </div>
          {loading ? <div className="text-muted">{t(language, "loading")}...</div> : items.length === 0 ? <div className="text-muted">{t(language, "noQuotes")}</div> : (
            <div className="table-responsive">
              <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                <thead>
                  <tr>
                    <th>{t(language, "quoteNumber")}</th>
                    <th>{t(language, "buyer")}</th>
                    <th>{t(language, "validUntil")}</th>
                    <th>{t(language, "status")}</th>
                    <th>{t(language, "total")}</th>
                    <th className="text-right">{t(language, "actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((quote) => (
                    <tr key={quote.id}>
                      <td>
                        <div className="entity-cell">
                          <span className="entity-avatar avatar-product"><FileSignature size={18} /></span>
                          <span><strong>{quote.quoteNumber}</strong><small>{new Date(quote.createdAtUtc).toLocaleString()}</small></span>
                        </div>
                      </td>
                      <td>{quote.document?.buyer?.["BT-26"] || "-"}</td>
                      <td>{new Date(quote.validUntilUtc).toLocaleDateString()}</td>
                      <td><span className={`status-pill ${quoteStatusClass(quote.status)}`}>{quoteStatusLabel(language, quote.status)}</span></td>
                      <td>{Number(quote.totalAmount || 0).toFixed(2)} {quote.document?.core?.["BT-5"] || "EUR"}</td>
                      <td className="text-right"><Link className="btn btn-icon btn-soft-primary" to={`/quotes/${quote.id}`} title={t(language, "open")}><Eye size={15} /></Link></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </>
  );
}

function SummaryTile({ label, value }) {
  return <div className="summary-tile"><span>{label}</span><strong>{value}</strong></div>;
}

function countStatus(items, statuses) {
  return items.filter((item) => statuses.includes(item.status || "Draft")).length;
}

export function quoteStatusLabel(language, status = "Draft") {
  return t(language, `quoteStatus${status}`);
}

export function quoteStatusClass(status = "Draft") {
  return {
    Draft: "status-muted",
    Sent: "status-info",
    Accepted: "status-ready",
    Declined: "status-risk",
    Converted: "status-ready"
  }[status] || "status-muted";
}
