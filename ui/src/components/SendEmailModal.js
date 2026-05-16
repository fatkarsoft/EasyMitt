import { useState } from "react";
import { Mail, X } from "lucide-react";
import { t } from "../i18n.js";

export default function SendEmailModal({
  open,
  language,
  documentType,
  defaultSubject = "",
  defaultBody = "",
  onSend,
  onClose,
}) {
  const [toEmail, setToEmail] = useState("");
  const [subject, setSubject] = useState(defaultSubject);
  const [body, setBody] = useState(defaultBody);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState(null);

  if (!open) return null;

  const isInvoice = documentType === "invoice";

  async function handleSubmit(e) {
    e.preventDefault();
    if (!toEmail.trim()) return;
    setSending(true);
    setError(null);
    try {
      await onSend({ toEmail: toEmail.trim(), subject: subject.trim(), body: body.trim() });
      setToEmail("");
      setSubject(defaultSubject);
      setBody(defaultBody);
      onClose();
    } catch (err) {
      setError(err?.message || t(language, "emailFailed"));
    } finally {
      setSending(false);
    }
  }

  return (
    <div className="confirm-overlay" role="presentation">
      <div className="confirm-card" role="dialog" aria-modal="true" style={{ maxWidth: 520 }}>
        <div className="confirm-card-header">
          <div className="confirm-header-lockup">
            <span className="confirm-icon confirm-icon-primary"><Mail size={22} /></span>
            <span className="confirm-eyebrow">{t(language, "sendEmail")}</span>
          </div>
          <button type="button" className="confirm-close" onClick={onClose} disabled={sending}>
            <X size={18} />
          </button>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="confirm-card-body" style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            {error && <div className="alert alert-danger mb-0">{error}</div>}
            {isInvoice && (
              <div className="alert alert-info mb-0" style={{ fontSize: 13 }}>
                <Mail size={14} className="me-1" />
                {t(language, "emailAttachmentZugferd")}
              </div>
            )}
            <div>
              <label className="form-label">{t(language, "emailTo")}</label>
              <input
                type="email"
                className="form-control"
                value={toEmail}
                onChange={(e) => setToEmail(e.target.value)}
                required
                disabled={sending}
                placeholder="kunde@beispiel.de"
              />
            </div>
            <div>
              <label className="form-label">{t(language, "emailSubject")}</label>
              <input
                type="text"
                className="form-control"
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                disabled={sending}
              />
            </div>
            <div>
              <label className="form-label">{t(language, "emailBody")}</label>
              <textarea
                className="form-control"
                rows={5}
                value={body}
                onChange={(e) => setBody(e.target.value)}
                disabled={sending}
              />
            </div>
          </div>
          <div className="confirm-card-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={sending}>
              {t(language, "cancel")}
            </button>
            <button type="submit" className="btn btn-primary" disabled={sending || !toEmail.trim()}>
              {sending ? t(language, "emailSending") : t(language, "sendEmail")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
