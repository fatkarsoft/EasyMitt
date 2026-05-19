import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { portalApi } from "../api/portal.js";
import { ApiError } from "../api/client.js";
import { t } from "../i18n.js";
import { QuoteStatusBadge } from "./PortalDashboard.js";

function fmt(amount) {
  return Number(amount || 0).toFixed(2);
}

export default function PortalQuotes({ language, session }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);

  useEffect(() => {
    let alive = true;
    portalApi.listQuotes()
      .then((data) => { if (alive) setItems(Array.isArray(data) ? data : []); })
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  return (
    <>
      <PageTitle title={t(language, "portalQuotes")} parent={session?.companyName || "EasyMitt"} />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="card ops-card">
        <div className="card-body">
          {loading ? <div className="text-muted">{t(language, "loading")}...</div> : items.length === 0 ? (
            <div className="text-muted">{t(language, "portalNoQuotes")}</div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead><tr>
                  <th>{t(language, "portalQuoteNumber")}</th>
                  <th>{t(language, "portalStatus")}</th>
                  <th>{t(language, "portalValidUntil")}</th>
                  <th className="text-end">{t(language, "portalTotalAmount")}</th>
                </tr></thead>
                <tbody>
                  {items.map((quote) => (
                    <tr key={quote.id}>
                      <td><Link to={`/portal/quotes/${quote.id}`}>{quote.quoteNumber}</Link></td>
                      <td><QuoteStatusBadge language={language} status={quote.status} /></td>
                      <td>{new Date(quote.validUntilUtc).toLocaleDateString()}</td>
                      <td className="text-end">{fmt(quote.totalAmount)} EUR</td>
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
