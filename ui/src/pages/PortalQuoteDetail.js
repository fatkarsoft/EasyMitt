import { useEffect, useState } from "react";
import { ArrowLeft, CheckCircle2, ThumbsDown } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { portalApi } from "../api/portal.js";
import { ApiError } from "../api/client.js";
import { t } from "../i18n.js";
import { QuoteStatusBadge } from "./PortalDashboard.js";

function fmt(amount) {
  return Number(amount || 0).toFixed(2);
}

function parseDocument(payloadJson) {
  if (!payloadJson) return null;
  try {
    return JSON.parse(payloadJson);
  } catch {
    return null;
  }
}

export default function PortalQuoteDetail({ language, session }) {
  const { id } = useParams();
  const [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);
  const [working, setWorking] = useState("");

  useEffect(() => {
    let alive = true;
    portalApi.getQuote(id)
      .then((data) => { if (alive) setDetail(data); })
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, [id]);

  async function respond(action) {
    setWorking(action);
    setMessage(null);
    try {
      const updated = action === "accept" ? await portalApi.acceptQuote(id) : await portalApi.declineQuote(id);
      setDetail((prev) => prev ? { ...prev, summary: updated } : prev);
      setMessage(["success", t(language, action === "accept" ? "portalQuoteAccepted" : "portalQuoteDeclined")]);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    } finally {
      setWorking("");
    }
  }

  const summary = detail?.summary;
  const doc = parseDocument(detail?.payloadJson);
  const lines = doc?.lines || [];
  const canRespond = summary && (summary.status === "Sent");

  return (
    <>
      <PageTitle
        title={t(language, "portalQuoteDetail")}
        parent={session?.companyName || "EasyMitt"}
        action={<Link className="btn btn-secondary" to="/portal/quotes"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>}
      />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      {loading ? <div className="card"><div className="card-body text-muted">{t(language, "loading")}...</div></div> : !summary ? (
        <div className="card"><div className="card-body text-muted">{t(language, "portalQuoteNotFound")}</div></div>
      ) : (
        <>
          <div className="row">
            <Info label={t(language, "portalQuoteNumber")} value={summary.quoteNumber} />
            <Info label={t(language, "portalStatus")} value={<QuoteStatusBadge language={language} status={summary.status} />} />
            <Info label={t(language, "portalValidUntil")} value={new Date(summary.validUntilUtc).toLocaleDateString()} />
            <Info label={t(language, "portalTotalAmount")} value={`${fmt(summary.totalAmount)} EUR`} />
          </div>
          <div className="card ops-card">
            <div className="card-body">
              <div className="page-action-group mb-3">
                <button className="btn btn-outline-success" type="button" disabled={!canRespond || !!working} onClick={() => respond("accept")}>
                  <CheckCircle2 size={16} /> {t(language, "portalAcceptQuote")}
                </button>
                <button className="btn btn-outline-danger" type="button" disabled={!canRespond || !!working} onClick={() => respond("decline")}>
                  <ThumbsDown size={16} /> {t(language, "portalDeclineQuote")}
                </button>
              </div>

              <h5 className="mb-3">{t(language, "portalLineItems")}</h5>
              {lines.length === 0 ? <div className="text-muted">—</div> : (
                <div className="table-responsive">
                  <table className="table table-sm align-middle mb-0">
                    <thead><tr>
                      <th>{t(language, "portalLineName")}</th>
                      <th className="text-end">{t(language, "portalLineQuantity")}</th>
                      <th className="text-end">{t(language, "portalLineUnitPrice")}</th>
                      <th className="text-end">{t(language, "portalLineVat")}</th>
                      <th className="text-end">{t(language, "portalLineTotal")}</th>
                    </tr></thead>
                    <tbody>
                      {lines.map((line, idx) => {
                        const qty = Number(line["BT-129"] || 0);
                        const unitNet = Number(line["BT-146"] || 0);
                        const vat = Number(line["BT-152"] || 0);
                        const total = qty * unitNet;
                        return (
                          <tr key={idx}>
                            <td>{line["BT-153"] || line["BT-154"] || "—"}</td>
                            <td className="text-end">{qty}</td>
                            <td className="text-end">{fmt(unitNet)}</td>
                            <td className="text-end">{vat}%</td>
                            <td className="text-end">{fmt(total)}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </>
  );
}

function Info({ label, value }) {
  return (
    <div className="col-md-3 col-6">
      <div className="card ops-card">
        <div className="card-body">
          <p className="text-muted mb-1">{label}</p>
          <h5 className="mb-0">{value}</h5>
        </div>
      </div>
    </div>
  );
}
