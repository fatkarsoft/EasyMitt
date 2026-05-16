import { useEffect, useState } from "react";
import { ArrowLeft, Ban, BellRing, CheckCircle2, Clock, CreditCard, Mail, Send, Stamp } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import ConfirmDialog from "../components/ConfirmDialog.js";
import SendEmailModal from "../components/SendEmailModal.js";
import { ApiError } from "../api/client.js";
import { invoicesApi } from "../api/invoices.js";
import { paymentsApi } from "../api/payments.js";
import { dunningApi } from "../api/dunning.js";
import { emailApi } from "../api/email.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";
import { getDocument } from "../utils/invoice.js";

export default function InvoiceDetail() {
  const { id } = useParams();
  const { language, canWrite } = useAuth();
  const [draft, setDraft] = useState(null);
  const [paymentSummary, setPaymentSummary] = useState(null);
  const [reminders, setReminders] = useState([]);
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState("");
  const [confirmCancel, setConfirmCancel] = useState(false);
  const [emailOpen, setEmailOpen] = useState(false);
  const [emailLogs, setEmailLogs] = useState([]);
  const document = getDocument(draft);

  useEffect(() => {
    let alive = true;
    Promise.all([
      invoicesApi.getDraft(id),
      paymentsApi.invoiceSummary(id).catch(() => null),
      dunningApi.invoiceReminders(id).catch(() => []),
      emailApi.getInvoiceLogs(id).catch(() => []),
    ])
      .then(([draftData, paymentData, reminderData, logData]) => {
        if (!alive) return;
        setDraft(draftData);
        setPaymentSummary(paymentData);
        setReminders(reminderData || []);
        setEmailLogs(Array.isArray(logData) ? logData : []);
      })
      .catch((err) => { if (alive) setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]); });
    return () => { alive = false; };
  }, [id]);

  async function handleSendEmail(body) {
    await emailApi.sendInvoice(id, body);
    setMessage(["success", t(language, "emailSent")]);
    emailApi.getInvoiceLogs(id).then((logs) => setEmailLogs(Array.isArray(logs) ? logs : [])).catch(() => {});
  }

  async function run(key, action) {
    if (!document) return;
    setLoading(key);
    setMessage(null);
    try {
      await action(document);
      if (key === "submit") setMessage(["success", t(language, "submitted")]);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    } finally {
      setLoading("");
    }
  }

  async function transition(key, action) {
    setLoading(key);
    setMessage(null);
    try {
      const updated = await action(id);
      setDraft(updated);
      setMessage(["success", t(language, "invoiceStatusUpdated")]);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    } finally {
      setLoading("");
      setConfirmCancel(false);
    }
  }

  return (
    <>
      <PageTitle title={t(language, "detail")} action={<Link className="btn btn-secondary" to="/invoices"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      {!draft || !document ? <div className="card"><div className="card-body text-muted">{t(language, "loading")}...</div></div> : (
        <>
          <div className="row">
            <Info label="ID" value={draft.id} />
            <Info label={t(language, "hash")} value={draft.canonicalSha256Hex || draft.hash || "-"} />
            <Info label={t(language, "archiveKey")} value={draft.archiveObjectKey || "-"} />
            <Info label={t(language, "createdAt")} value={draft.createdAtUtc ? new Date(draft.createdAtUtc).toLocaleString() : "-"} />
          </div>
          <div className="card">
            <div className="card-body">
              <div className="button-items mb-4">
                <button className="btn btn-secondary" disabled={!!loading} onClick={() => run("xml", invoicesApi.exportXrechnung)}>{loading === "xml" ? `${t(language, "loading")}...` : t(language, "exportXml")}</button>
                <button className="btn btn-secondary" disabled={!!loading} onClick={() => run("pdf", invoicesApi.exportZugferd)}>{loading === "pdf" ? `${t(language, "loading")}...` : t(language, "exportPdf")}</button>
                <button className="btn btn-primary" disabled={!canWrite || !!loading} onClick={() => run("submit", invoicesApi.submitPeppol)}>{loading === "submit" ? `${t(language, "loading")}...` : t(language, "submit")}</button>
                {canWrite && <button className="btn btn-outline-primary" disabled={!!loading} onClick={() => setEmailOpen(true)}><Mail size={16} /> {t(language, "sendEmail")}</button>}
              </div>
              <div className="invoice-lifecycle-panel mb-4">
                <div>
                  <span className={`status-pill ${statusClass(draft.status)}`}>{statusLabel(language, draft.status)}</span>
                  <p className="text-muted mb-0 mt-2">{t(language, "invoiceLifecycleHint")}</p>
                </div>
                <div className="page-action-group">
                  {canTransition(draft.status, "Issued") && <button className="btn btn-outline-primary" disabled={!canWrite || !!loading} onClick={() => transition("issue", invoicesApi.issueDraft)}><Stamp size={16} /> {t(language, "markIssued")}</button>}
                  {canTransition(draft.status, "Sent") && <button className="btn btn-outline-primary" disabled={!canWrite || !!loading} onClick={() => transition("send", invoicesApi.sendDraft)}><Send size={16} /> {t(language, "markSent")}</button>}
                  {canTransition(draft.status, "Paid") && <button className="btn btn-outline-success" disabled={!canWrite || !!loading} onClick={() => transition("paid", invoicesApi.payDraft)}><CheckCircle2 size={16} /> {t(language, "markPaid")}</button>}
                  {canTransition(draft.status, "Overdue") && <button className="btn btn-outline-danger" disabled={!canWrite || !!loading} onClick={() => transition("overdue", invoicesApi.markOverdue)}><Clock size={16} /> {t(language, "markOverdue")}</button>}
                  {canTransition(draft.status, "Cancelled") && <button className="btn btn-outline-danger" disabled={!canWrite || !!loading} onClick={() => setConfirmCancel(true)}><Ban size={16} /> {t(language, "cancelInvoice")}</button>}
                </div>
              </div>
              {paymentSummary && (
                <div className="invoice-lifecycle-panel mb-4">
                  <div>
                    <span className={`status-pill ${paymentSummary.paymentStatus === "Paid" ? "status-ready" : paymentSummary.paymentStatus === "PartiallyPaid" ? "status-info" : "status-muted"}`}>
                      <CreditCard size={14} /> {t(language, `paymentInvoiceStatus${paymentSummary.paymentStatus}`)}
                    </span>
                    <p className="text-muted mb-0 mt-2">{t(language, "invoicePaymentHint")}</p>
                  </div>
                  <div className="page-action-group">
                    <span className="status-pill status-muted">{t(language, "paidAmount")}: {Number(paymentSummary.paidAmount || 0).toFixed(2)}</span>
                    <span className="status-pill status-muted">{t(language, "openAmount")}: {Number(paymentSummary.openAmount || 0).toFixed(2)}</span>
                  </div>
                </div>
              )}
              <div className="invoice-lifecycle-panel mb-4">
                <div>
                  <span className="status-pill status-info"><BellRing size={14} /> {t(language, "reminderHistory")}</span>
                  <p className="text-muted mb-0 mt-2">{t(language, "reminderHistoryHint")}</p>
                </div>
                <div className="page-action-group">
                  {reminders.length === 0 ? <span className="status-pill status-muted">{t(language, "noReminders")}</span> : reminders.map((reminder) => (
                    <span className="status-pill status-muted" key={reminder.id}>{reminderLevelLabel(language, reminder.level)} · {new Date(reminder.createdAtUtc).toLocaleDateString()}</span>
                  ))}
                  <Link className="btn btn-outline-primary" to="/dunning"><BellRing size={16} /> {t(language, "dunning")}</Link>
                </div>
              </div>
              <div className="row">
                <Info label={t(language, "invoiceNumber")} value={document.core["BT-1"] || "-"} small />
                <Info label={t(language, "buyer")} value={document.buyer["BT-26"] || "-"} small />
                <Info label={t(language, "issueDate")} value={document.core["BT-2"] || "-"} small />
                <Info label={t(language, "total")} value={`${document.core["BT-112"]} ${document.core["BT-5"]}`} small />
              </div>
              <div className="invoice-lifecycle-panel mb-4">
                <div>
                  <span className="status-pill status-info"><Mail size={14} /> {t(language, "emailDeliveryLogs")}</span>
                  <p className="text-muted mb-0 mt-2">{t(language, "emailDeliveryLogsHint")}</p>
                </div>
                <div className="page-action-group" style={{ flexWrap: "wrap" }}>
                  {emailLogs.length === 0
                    ? <span className="status-pill status-muted">{t(language, "noEmailLogs")}</span>
                    : emailLogs.map((log) => (
                      <span className={`status-pill ${log.status === "Sent" ? "status-ready" : "status-risk"}`} key={log.id}>
                        <Mail size={12} /> {log.toEmail} · {new Date(log.createdAtUtc).toLocaleString()} · {t(language, `emailStatus_${log.status}`)}
                      </span>
                    ))}
                </div>
              </div>
              <pre className="json-preview">{JSON.stringify(document, null, 2)}</pre>
            </div>
          </div>
        </>
      )}
      <ConfirmDialog
        open={confirmCancel}
        title={t(language, "cancelInvoice")}
        eyebrow={t(language, "areYouSure")}
        message={t(language, "confirmCancelInvoice")}
        confirmLabel={t(language, "cancelInvoice")}
        cancelLabel={t(language, "cancel")}
        onConfirm={() => transition("cancel", invoicesApi.cancelDraft)}
        onCancel={() => setConfirmCancel(false)}
      />
      <SendEmailModal
        open={emailOpen}
        language={language}
        documentType="invoice"
        defaultSubject={document ? `Rechnung ${document.core?.["BT-1"] || ""}` : ""}
        defaultBody=""
        onSend={handleSendEmail}
        onClose={() => setEmailOpen(false)}
      />
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

function statusLabel(language, status = "Draft") {
  return t(language, `invoiceStatus${status}`);
}

function statusClass(status = "Draft") {
  return {
    Draft: "status-muted",
    Issued: "status-info",
    Sent: "status-info",
    PartiallyPaid: "status-info",
    Paid: "status-ready",
    Overdue: "status-risk",
    Cancelled: "status-muted"
  }[status] || "status-muted";
}

function reminderLevelLabel(language, level = 0) {
  const key = {
    1: "friendlyReminder",
    2: "firstMahnung",
    3: "secondMahnung",
    4: "finalNotice"
  }[level] || "noReminder";
  return t(language, key);
}

function canTransition(current = "Draft", next) {
  return {
    Draft: ["Issued"],
    Issued: ["Sent", "Paid", "Overdue", "Cancelled"],
    Sent: ["Paid", "Overdue", "Cancelled"],
    PartiallyPaid: ["Paid", "Overdue", "Cancelled"],
    Overdue: ["Paid", "Cancelled"],
    Paid: [],
    Cancelled: []
  }[current]?.includes(next);
}
