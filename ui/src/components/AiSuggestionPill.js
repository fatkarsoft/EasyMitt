import { Check, Sparkles, X } from "lucide-react";
import { t } from "../i18n.js";

function confidenceColor(confidence) {
  if (confidence >= 0.85) return "success";
  if (confidence >= 0.65) return "warning";
  return "secondary";
}

export default function AiSuggestionPill({
  language,
  label,
  value,
  confidence = 0,
  rationale,
  status = "pending",
  onAccept,
  onReject,
  disabled = false,
  acceptLabel,
  rejectLabel,
  extra = null
}) {
  const pct = Math.round((Number(confidence) || 0) * 100);
  const color = confidenceColor(confidence);
  const decided = status === "Accepted" || status === "Rejected";

  return (
    <div className={`ai-suggestion-pill ai-suggestion-pill-${color} ${decided ? "is-decided" : ""}`} role="region" aria-label={t(language, "aiSuggestionExplain")}>
      <div className="ai-pill-main">
        <span className="ai-pill-icon"><Sparkles size={14} /></span>
        <div className="ai-pill-body">
          <span className="ai-pill-label">{label || t(language, "aiSuggestionPillSuggestion")}</span>
          <strong className="ai-pill-value">{value}</strong>
          {rationale && <span className="ai-pill-rationale" title={rationale}>· {rationale}</span>}
        </div>
        <span className={`ai-pill-confidence text-${color}`} title={t(language, "aiSuggestionPillConfidence")}>{pct}%</span>
      </div>
      {extra}
      {!decided && (onAccept || onReject) && (
        <div className="ai-pill-actions">
          {onAccept && (
            <button type="button" className={`btn btn-sm btn-${color}`} disabled={disabled} onClick={onAccept}>
              <Check size={13} /> {acceptLabel || t(language, "aiSuggestionPillAccept")}
            </button>
          )}
          {onReject && (
            <button type="button" className="btn btn-sm btn-light" disabled={disabled} onClick={onReject}>
              <X size={13} /> {rejectLabel || t(language, "aiSuggestionPillReject")}
            </button>
          )}
        </div>
      )}
      {status === "Accepted" && <span className="ai-pill-status text-success">{t(language, "aiSuggestionAccepted")}</span>}
      {status === "Rejected" && <span className="ai-pill-status text-muted">{t(language, "aiSuggestionRejected")}</span>}
    </div>
  );
}
