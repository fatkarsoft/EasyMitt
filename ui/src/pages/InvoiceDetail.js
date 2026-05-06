import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { ApiError } from "../api/client.js";
import { invoicesApi } from "../api/invoices.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";
import { getDocument } from "../utils/invoice.js";

export default function InvoiceDetail() {
  const { id } = useParams();
  const { language, canWrite } = useAuth();
  const [draft, setDraft] = useState(null);
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState("");
  const document = getDocument(draft);

  useEffect(() => {
    invoicesApi.getDraft(id)
      .then(setDraft)
      .catch((err) => setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]));
  }, [id]);

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

  return (
    <>
      <PageTitle title={t(language, "detail")} />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      {!draft || !document ? <div className="card"><div className="card-body text-muted">{t(language, "loading")}...</div></div> : (
        <>
          <div className="row">
            <Info label="ID" value={draft.id} />
            <Info label={t(language, "hash")} value={draft.hash || "-"} />
            <Info label={t(language, "archiveKey")} value={draft.archiveObjectKey || "-"} />
            <Info label={t(language, "createdAt")} value={draft.createdAt ? new Date(draft.createdAt).toLocaleString() : "-"} />
          </div>
          <div className="card">
            <div className="card-body">
              <div className="button-items mb-4">
                <button className="btn btn-secondary" disabled={!!loading} onClick={() => run("xml", invoicesApi.exportXrechnung)}>{loading === "xml" ? `${t(language, "loading")}...` : t(language, "exportXml")}</button>
                <button className="btn btn-secondary" disabled={!!loading} onClick={() => run("pdf", invoicesApi.exportZugferd)}>{loading === "pdf" ? `${t(language, "loading")}...` : t(language, "exportPdf")}</button>
                <button className="btn btn-primary" disabled={!canWrite || !!loading} onClick={() => run("submit", invoicesApi.submitPeppol)}>{loading === "submit" ? `${t(language, "loading")}...` : t(language, "submit")}</button>
              </div>
              <div className="row">
                <Info label={t(language, "invoiceNumber")} value={document.core["BT-1"] || "-"} small />
                <Info label={t(language, "buyer")} value={document.buyer["BT-26"] || "-"} small />
                <Info label={t(language, "issueDate")} value={document.core["BT-2"] || "-"} small />
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
