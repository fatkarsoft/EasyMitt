import { useEffect, useMemo, useState } from "react";
import { CircleDollarSign, FileSignature, FileText, Receipt, ShieldAlert } from "lucide-react";
import { Link } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import { portalApi } from "../api/portal.js";
import { ApiError } from "../api/client.js";
import { t } from "../i18n.js";

function fmt(amount) {
  return Number(amount || 0).toFixed(2);
}

export default function PortalDashboard({ language, session }) {
  const [invoices, setInvoices] = useState([]);
  const [quotes, setQuotes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);

  useEffect(() => {
    let alive = true;
    Promise.all([
      portalApi.listInvoices(),
      portalApi.listQuotes()
    ])
      .then(([inv, quo]) => {
        if (!alive) return;
        setInvoices(Array.isArray(inv) ? inv : []);
        setQuotes(Array.isArray(quo) ? quo : []);
      })
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, []);

  const totals = useMemo(() => {
    const totalOpen = invoices.reduce((sum, inv) => sum + Number(inv.amountOpen || 0), 0);
    const totalOverdue = invoices.filter((x) => x.isOverdue).reduce((sum, inv) => sum + Number(inv.amountOpen || 0), 0);
    const openQuotes = quotes.filter((q) => q.status === "Sent").length;
    return { totalOpen, totalOverdue, openQuotes };
  }, [invoices, quotes]);

  return (
    <>
      <PageTitle title={t(language, "portalOverview")} parent={session?.companyName || "EasyMitt"} />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}

      <div className="row">
        <KpiCard icon={<Receipt size={20} />} label={t(language, "portalInvoiceCount")} value={invoices.length} />
        <KpiCard icon={<CircleDollarSign size={20} />} label={t(language, "portalAmountOpen")} value={`${fmt(totals.totalOpen)} EUR`} />
        <KpiCard icon={<ShieldAlert size={20} />} label={t(language, "portalAmountOverdue")} value={`${fmt(totals.totalOverdue)} EUR`} tone="danger" />
        <KpiCard icon={<FileSignature size={20} />} label={t(language, "portalOpenQuotes")} value={totals.openQuotes} />
      </div>

      <div className="row">
        <div className="col-lg-7">
          <div className="card ops-card">
            <div className="card-body">
              <div className="d-flex justify-content-between align-items-center mb-3">
                <h4 className="card-title mb-0"><FileText size={16} className="me-1" /> {t(language, "portalRecentInvoices")}</h4>
                <Link to="/portal/invoices" className="btn btn-sm btn-outline-secondary">{t(language, "portalSeeAll")}</Link>
              </div>
              {loading ? <div className="text-muted">{t(language, "loading")}...</div> : invoices.length === 0 ? (
                <div className="text-muted">{t(language, "portalNoInvoices")}</div>
              ) : (
                <div className="table-responsive">
                  <table className="table table-sm align-middle mb-0">
                    <thead><tr>
                      <th>{t(language, "portalInvoiceNumber")}</th>
                      <th>{t(language, "portalIssueDate")}</th>
                      <th>{t(language, "portalStatus")}</th>
                      <th className="text-end">{t(language, "portalAmountOpen")}</th>
                    </tr></thead>
                    <tbody>
                      {invoices.slice(0, 6).map((invoice) => (
                        <tr key={invoice.id}>
                          <td><Link to={`/portal/invoices/${invoice.id}`}>{invoice.invoiceNumber}</Link></td>
                          <td>{invoice.issueDate || "—"}</td>
                          <td><InvoiceStatusBadge language={language} invoice={invoice} /></td>
                          <td className="text-end">{fmt(invoice.amountOpen)} EUR</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
        <div className="col-lg-5">
          <div className="card ops-card">
            <div className="card-body">
              <div className="d-flex justify-content-between align-items-center mb-3">
                <h4 className="card-title mb-0"><FileSignature size={16} className="me-1" /> {t(language, "portalRecentQuotes")}</h4>
                <Link to="/portal/quotes" className="btn btn-sm btn-outline-secondary">{t(language, "portalSeeAll")}</Link>
              </div>
              {loading ? <div className="text-muted">{t(language, "loading")}...</div> : quotes.length === 0 ? (
                <div className="text-muted">{t(language, "portalNoQuotes")}</div>
              ) : (
                <div className="table-responsive">
                  <table className="table table-sm align-middle mb-0">
                    <thead><tr>
                      <th>{t(language, "portalQuoteNumber")}</th>
                      <th>{t(language, "portalStatus")}</th>
                      <th className="text-end">{t(language, "portalTotalAmount")}</th>
                    </tr></thead>
                    <tbody>
                      {quotes.slice(0, 6).map((quote) => (
                        <tr key={quote.id}>
                          <td><Link to={`/portal/quotes/${quote.id}`}>{quote.quoteNumber}</Link></td>
                          <td><QuoteStatusBadge language={language} status={quote.status} /></td>
                          <td className="text-end">{fmt(quote.totalAmount)} EUR</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

function KpiCard({ icon, label, value, tone }) {
  return (
    <div className="col-md-6 col-xl-3">
      <div className={`card ops-card kpi-card ${tone ? `kpi-${tone}` : ""}`}>
        <div className="card-body">
          <div className="d-flex align-items-center justify-content-between">
            <div>
              <p className="text-muted mb-1">{label}</p>
              <h4 className="mb-0">{value}</h4>
            </div>
            <div className="kpi-icon">{icon}</div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function InvoiceStatusBadge({ language, invoice }) {
  if (invoice.isOverdue) return <span className="status-pill status-overdue">{t(language, "portalStatusOverdue")}</span>;
  const map = {
    Issued: ["status-pill status-issued", "portalStatusIssued"],
    Sent: ["status-pill status-sent", "portalStatusSent"],
    PartiallyPaid: ["status-pill status-partial", "portalStatusPartiallyPaid"],
    Paid: ["status-pill status-paid", "portalStatusPaid"],
    Overdue: ["status-pill status-overdue", "portalStatusOverdue"],
    Cancelled: ["status-pill status-cancelled", "portalStatusCancelled"]
  };
  const [cls, key] = map[invoice.status] || ["status-pill", invoice.status];
  return <span className={cls}>{t(language, key)}</span>;
}

export function QuoteStatusBadge({ language, status }) {
  const map = {
    Sent: ["status-pill status-sent", "portalStatusSent"],
    Accepted: ["status-pill status-paid", "portalStatusAccepted"],
    Declined: ["status-pill status-cancelled", "portalStatusDeclined"],
    Converted: ["status-pill status-issued", "portalStatusConverted"]
  };
  const [cls, key] = map[status] || ["status-pill", status];
  return <span className={cls}>{t(language, key)}</span>;
}
