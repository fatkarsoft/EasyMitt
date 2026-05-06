import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { invoicesApi } from "../api/invoices.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";
import { getDocument, getDraftIds } from "../utils/invoice.js";

export default function Drafts() {
  const { language } = useAuth();
  const [drafts, setDrafts] = useState([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let alive = true;
    Promise.all(getDraftIds().map((id) => invoicesApi.getDraft(id)))
      .then((records) => alive && setDrafts(records))
      .catch((err) => alive && setError(err instanceof ApiError ? err.message : "Load failed"))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  return (
    <>
      <PageTitle title={t(language, "invoiceDrafts")} action={<Link className="btn btn-primary" to="/invoices/new">{t(language, "newInvoice")}</Link>} />
      {error && <div className="alert alert-danger">{error}</div>}
      <div className="card">
        <div className="card-body">
          {loading ? <div className="text-muted">{t(language, "loading")}...</div> : drafts.length === 0 ? <div className="text-muted">{t(language, "noDrafts")}</div> : (
            <div className="table-responsive">
              <table className="table table-centered table-nowrap mb-0">
                <thead className="thead-light">
                  <tr>
                    <th>{t(language, "invoiceNumber")}</th>
                    <th>{t(language, "buyer")}</th>
                    <th>{t(language, "issueDate")}</th>
                    <th>{t(language, "hash")}</th>
                    <th>{t(language, "createdAt")}</th>
                    <th>{t(language, "actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {drafts.map((draft) => {
                    const document = getDocument(draft);
                    return (
                      <tr key={draft.id}>
                        <td>{document?.core?.["BT-1"] || "-"}</td>
                        <td>{document?.buyer?.["BT-26"] || "-"}</td>
                        <td>{document?.core?.["BT-2"] || "-"}</td>
                        <td className="text-monospace small text-truncate hash-cell">{draft.hash || "-"}</td>
                        <td>{draft.createdAt ? new Date(draft.createdAt).toLocaleString() : "-"}</td>
                        <td><Link className="btn btn-sm btn-outline-primary" to={`/invoices/${draft.id}`}>{t(language, "open")}</Link></td>
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
  );
}
