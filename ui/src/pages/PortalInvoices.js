import { useEffect, useState } from "react";
import { Download } from "lucide-react";
import { Link } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { portalApi } from "../api/portal.js";
import { ApiError } from "../api/client.js";
import { t } from "../i18n.js";
import { InvoiceStatusBadge } from "./PortalDashboard.js";

function fmt(amount) {
  return Number(amount || 0).toFixed(2);
}

export default function PortalInvoices({ language, session }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);

  useEffect(() => {
    let alive = true;
    portalApi.listInvoices()
      .then((data) => { if (alive) setItems(Array.isArray(data) ? data : []); })
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  async function downloadPdf(invoice) {
    try {
      await portalApi.downloadInvoicePdf(invoice.id, `Rechnung-${invoice.invoiceNumber}.pdf`);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Download failed"]);
    }
  }

  return (
    <>
      <PageTitle title={t(language, "portalInvoices")} parent={session?.companyName || "EasyMitt"} />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="card ops-card">
        <div className="card-body">
          {loading ? <div className="text-muted">{t(language, "loading")}...</div> : items.length === 0 ? (
            <div className="text-muted">{t(language, "portalNoInvoices")}</div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead><tr>
                  <th>{t(language, "portalInvoiceNumber")}</th>
                  <th>{t(language, "portalIssueDate")}</th>
                  <th>{t(language, "portalStatus")}</th>
                  <th className="text-end">{t(language, "portalTotalAmount")}</th>
                  <th className="text-end">{t(language, "portalAmountPaid")}</th>
                  <th className="text-end">{t(language, "portalAmountOpen")}</th>
                  <th className="text-end">{t(language, "actions")}</th>
                </tr></thead>
                <tbody>
                  {items.map((invoice) => (
                    <tr key={invoice.id}>
                      <td><Link to={`/portal/invoices/${invoice.id}`}>{invoice.invoiceNumber}</Link></td>
                      <td>{invoice.issueDate || "—"}</td>
                      <td><InvoiceStatusBadge language={language} invoice={invoice} /></td>
                      <td className="text-end">{fmt(invoice.totalAmount)} EUR</td>
                      <td className="text-end">{fmt(invoice.amountPaid)} EUR</td>
                      <td className="text-end">{fmt(invoice.amountOpen)} EUR</td>
                      <td className="text-end">
                        <button className="btn btn-sm btn-outline-secondary" type="button" onClick={() => downloadPdf(invoice)}>
                          <Download size={14} /> PDF
                        </button>
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
