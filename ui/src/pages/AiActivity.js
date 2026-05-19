import { useEffect, useState } from "react";
import { RefreshCw, RotateCcw, Sparkles } from "lucide-react";
import PageTitle from "../components/PageTitle.js";
import { ApiError } from "../api/client.js";
import { aiApi } from "../api/ai.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const SUGGESTION_TYPES = ["ExpenseCategory", "DatevAccount", "PaymentMatch", "InvoiceField"];
const STATUSES = ["Pending", "Accepted", "Rejected", "Superseded"];

function statusClass(status) {
  return {
    Accepted: "status-ready",
    Rejected: "status-risk",
    Pending: "status-info",
    Superseded: "status-muted"
  }[status] || "status-muted";
}

function formatPayload(payload) {
  if (!payload || typeof payload !== "object") return "";
  if (payload.category) return `${payload.category} · ${Math.round((payload.confidence || 0) * 100)}%`;
  if (payload.account) return `${payload.account}${payload.taxKey ? ` · ${payload.taxKey}` : ""} · ${Math.round((payload.confidence || 0) * 100)}%`;
  if (payload.invoiceNumber) return `${payload.invoiceNumber} · ${Math.round((payload.confidence || 0) * 100)}%`;
  if (payload.fieldCode) return `${payload.fieldCode} → ${payload.suggestedValue || ""}`;
  return "";
}

export default function AiActivity() {
  const { language, canWrite } = useAuth();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);
  const [filterType, setFilterType] = useState("");
  const [filterStatus, setFilterStatus] = useState("");

  useEffect(() => {
    let alive = true;
    aiApi.list({ suggestionType: filterType, status: filterStatus, take: 200 })
      .then((data) => { if (alive) setItems(Array.isArray(data) ? data : []); })
      .catch((err) => { if (alive) setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]); })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, [filterType, filterStatus]);

  async function reload() {
    setLoading(true);
    setMessage(null);
    try {
      const data = await aiApi.list({ suggestionType: filterType, status: filterStatus, take: 200 });
      setItems(Array.isArray(data) ? data : []);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  async function retry(id) {
    try {
      await aiApi.retry(id);
      setMessage(["success", t(language, "aiSuggestionRetry")]);
      await reload();
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Retry failed"]);
    }
  }

  return (
    <>
      <PageTitle
        title={t(language, "aiActivity")}
        action={<button className="btn btn-secondary" type="button" onClick={reload}><RefreshCw size={15} /> {t(language, "refresh")}</button>}
      />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}

      <div className="card ops-card">
        <div className="card-body py-2">
          <div className="row align-items-end g-2">
            <div className="col-md-3 col-sm-6">
              <label className="form-label font-size-12 mb-1">{t(language, "aiSuggestionType")}</label>
              <select className="form-control form-control-sm" value={filterType} onChange={(e) => setFilterType(e.target.value)}>
                <option value="">{t(language, "aiSuggestionAllTypes")}</option>
                {SUGGESTION_TYPES.map((tp) => <option key={tp} value={tp}>{t(language, `aiSuggestionType${tp}`)}</option>)}
              </select>
            </div>
            <div className="col-md-3 col-sm-6">
              <label className="form-label font-size-12 mb-1">{t(language, "aiSuggestionStatus")}</label>
              <select className="form-control form-control-sm" value={filterStatus} onChange={(e) => setFilterStatus(e.target.value)}>
                <option value="">{t(language, "aiSuggestionAllStatuses")}</option>
                {STATUSES.map((s) => <option key={s} value={s}>{t(language, `aiSuggestion${s}`)}</option>)}
              </select>
            </div>
          </div>
        </div>
      </div>

      <div className="card ops-card">
        <div className="card-body">
          <div className="settings-section-header">
            <div>
              <h4 className="card-title mb-1"><Sparkles size={16} className="mr-2 text-warning" />{t(language, "aiRecentSuggestions")}</h4>
              <p className="text-muted mb-0">{t(language, "aiSuggestionsHint")}</p>
            </div>
          </div>
          {loading ? (
            <div className="text-muted mt-3">{t(language, "loading")}...</div>
          ) : items.length === 0 ? (
            <div className="text-muted mt-3">{t(language, "aiNoSuggestions")}</div>
          ) : (
            <div className="table-responsive mt-3">
              <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                <thead>
                  <tr>
                    <th>{t(language, "aiSuggestionType")}</th>
                    <th>{t(language, "aiSuggestionTarget")}</th>
                    <th>{t(language, "aiSuggestionPillSuggestion")}</th>
                    <th>{t(language, "aiSuggestionStatus")}</th>
                    <th>{t(language, "aiSuggestionCreatedAt")}</th>
                    <th>{t(language, "aiSuggestionDecidedBy")}</th>
                    <th className="text-right">{t(language, "actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <span className="status-pill status-info">
                          {t(language, `aiSuggestionType${item.suggestionType}`) || item.suggestionType}
                        </span>
                      </td>
                      <td className="font-size-12">{item.targetType}{item.targetId ? ` · ${String(item.targetId).slice(0, 8)}` : ""}</td>
                      <td className="font-size-12">{formatPayload(item.payload)}</td>
                      <td>
                        <span className={`status-pill ${statusClass(item.status)}`}>
                          {t(language, `aiSuggestion${item.status}`) || item.status}
                        </span>
                      </td>
                      <td className="font-size-12">{new Date(item.createdAtUtc).toLocaleString()}</td>
                      <td className="font-size-12">{item.decidedByUserEmail || "—"}</td>
                      <td className="text-right">
                        {item.status === "Rejected" && canWrite && (
                          <button className="btn btn-icon btn-soft-primary" type="button" title={t(language, "aiSuggestionRetry")} onClick={() => retry(item.id)}>
                            <RotateCcw size={14} />
                          </button>
                        )}
                      </td>
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
