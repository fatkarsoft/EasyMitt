import { useEffect, useState } from "react";
import { ArrowLeft, Download, FileCode2 } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { portalApi } from "../api/portal.js";
import { ApiError } from "../api/client.js";
import { t } from "../i18n.js";
import { InvoiceStatusBadge } from "./PortalDashboard.js";

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

export default function PortalInvoiceDetail({ language, session }) {
  const { id } = useParams();
  const [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);

  useEffect(() => {
    let alive = true;
    portalApi.getInvoice(id)
      .then((data) => { if (alive) setDetail(data); })
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, [id]);

  async function downloadPdf() {
    try {
      await portalApi.downloadInvoicePdf(id, `Rechnung-${detail?.summary?.invoiceNumber || id}.pdf`);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Download failed"]);
    }
  }
  async function downloadXml() {
    try {
      await portalApi.downloadInvoiceXml(id, `Rechnung-${detail?.summary?.invoiceNumber || id}.xml`);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Download failed"]);
    }
  }

  const summary = detail?.summary;
  const doc = parseDocument(detail?.payloadJson);
  const lines = doc?.lines || [];
  const payments = detail?.payments || [];

  return (
    <>
      <PageTitle
        title={t(language, "portalInvoiceDetail")}
        parent={session?.companyName || "EasyMitt"}
        action={<Link className="btn btn-secondary" to="/portal/invoices"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>}
      />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      {loading ? <div className="card"><div className="card-body text-muted">{t(language, "loading")}...</div></div> : !summary ? (
        <div className="card"><div className="card-body text-muted">{t(language, "portalInvoiceNotFound")}</div></div>
      ) : (
        <>
          <div className="row">
            <Info label={t(language, "portalInvoiceNumber")} value={summary.invoiceNumber} />
            <Info label={t(language, "portalIssueDate")} value={summary.issueDate || "—"} />
            <Info label={t(language, "portalStatus")} value={<InvoiceStatusBadge language={language} invoice={summary} />} />
            <Info label={t(language, "portalAmountOpen")} value={`${fmt(summary.amountOpen)} EUR`} />
          </div>
          <div className="card ops-card">
            <div className="card-body">
              <div className="page-action-group mb-3">
                <button className="btn btn-primary" type="button" onClick={downloadPdf}><Download size={16} /> {t(language, "portalDownloadPdf")}</button>
                <button className="btn btn-outline-secondary" type="button" onClick={downloadXml}><FileCode2 size={16} /> {t(language, "portalDownloadXml")}</button>
              </div>

              <h5 className="mb-3">{t(language, "portalLineItems")}</h5>
              {lines.length === 0 ? <div className="text-muted">—</div> : (
                <div className="table-responsive mb-4">
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

              <h5 className="mb-3">{t(language, "portalPayments")}</h5>
              {payments.length === 0 ? <div className="text-muted">{t(language, "portalNoPayments")}</div> : (
                <div className="table-responsive">
                  <table className="table table-sm align-middle mb-0">
                    <thead><tr>
                      <th>{t(language, "portalPaymentDate")}</th>
                      <th>{t(language, "portalPaymentDescription")}</th>
                      <th className="text-end">{t(language, "portalPaymentAmount")}</th>
                    </tr></thead>
                    <tbody>
                      {payments.map((row, idx) => (
                        <tr key={idx}>
                          <td>{row.bookingDate}</td>
                          <td>{row.description}</td>
                          <td className="text-end">{fmt(row.amount)} {row.currencyCode || "EUR"}</td>
                        </tr>
                      ))}
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
