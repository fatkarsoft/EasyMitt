import { AlertTriangle, X } from "lucide-react";

export default function ConfirmDialog({
  open,
  title,
  eyebrow,
  message,
  confirmLabel,
  cancelLabel,
  variant = "danger",
  onConfirm,
  onCancel
}) {
  if (!open) return null;

  return (
    <div className="confirm-overlay" role="presentation">
      <div className="confirm-card" role="dialog" aria-modal="true" aria-labelledby="confirm-title">
        <div className="confirm-card-header">
          <div className="confirm-header-lockup">
            <span className={`confirm-icon confirm-icon-${variant}`}><AlertTriangle size={22} /></span>
            {eyebrow && <span className="confirm-eyebrow">{eyebrow}</span>}
          </div>
          <button type="button" className="confirm-close" aria-label={cancelLabel} onClick={onCancel}>
            <X size={18} />
          </button>
        </div>
        <div className="confirm-card-body">
          <h5 id="confirm-title">{title}</h5>
          <p>{message}</p>
        </div>
        <div className="confirm-card-footer">
          <button type="button" className="btn btn-secondary" onClick={onCancel}>{cancelLabel}</button>
          <button type="button" className={`btn btn-${variant}`} onClick={onConfirm}>{confirmLabel}</button>
        </div>
      </div>
    </div>
  );
}
