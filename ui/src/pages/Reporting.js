import { BarChart3, CalendarRange, Coins, Euro, FileSpreadsheet, ReceiptText, RefreshCw, Search, Users } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import PageTitle from "../components/PageTitle.js";
import { ApiError } from "../api/client.js";
import { reportingApi } from "../api/reporting.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

function toLocalIso(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function todayIso() {
  return toLocalIso(new Date());
}

function yearStartIso() {
  const now = new Date();
  return toLocalIso(new Date(now.getFullYear(), 0, 1));
}

export default function Reporting() {
  const { language } = useAuth();
  const [from, setFrom] = useState(yearStartIso());
  const [to, setTo] = useState(todayIso());
  const [overview, setOverview] = useState(null);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);

  async function load(rangeFrom = from, rangeTo = to) {
    setLoading(true);
    setMessage(null);
    try {
      const data = await reportingApi.overview({ from: rangeFrom, to: rangeTo });
      setOverview(data);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let alive = true;
    reportingApi.overview({ from, to })
      .then((data) => alive && setOverview(data))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const summary = overview?.summary || null;
  const revenueByMonth = useMemo(() => overview?.revenueByMonth || [], [overview]);
  const vatSummary = overview?.vatSummary || [];
  const aging = useMemo(() => overview?.aging || [], [overview]);
  const topRevenueCustomers = overview?.topCustomersByRevenue || [];
  const topOverdueCustomers = overview?.topCustomersByOverdue || [];
  const datevCoverage = overview?.datevCoverage || null;
  const expenseByCategory = useMemo(() => overview?.expenseByCategory || [], [overview]);

  const monthMax = useMemo(() => Math.max(1, ...revenueByMonth.map((point) => Number(point.gross || 0))), [revenueByMonth]);
  const agingMax = useMemo(() => Math.max(1, ...aging.map((bucket) => Number(bucket.openAmount || 0))), [aging]);
  const expenseMax = useMemo(() => Math.max(1, ...expenseByCategory.map((row) => Number(row.totalAmount || 0))), [expenseByCategory]);

  const applyFilter = (event) => {
    event.preventDefault();
    load(from, to);
  };

  const resetFilter = () => {
    const nextFrom = yearStartIso();
    const nextTo = todayIso();
    setFrom(nextFrom);
    setTo(nextTo);
    load(nextFrom, nextTo);
  };

  return (
    <>
      <PageTitle title={t(language, "reporting")} action={<button className="btn btn-secondary" type="button" onClick={() => load(from, to)}><RefreshCw size={16} /> {t(language, "refresh")}</button>} />

      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}

      <div className="card ops-card">
        <div className="card-body">
          <form className="ops-toolbar" onSubmit={applyFilter}>
            <div>
              <h4 className="card-title mb-1">{t(language, "reportingFilters")}</h4>
              <p className="text-muted mb-0">{t(language, "reportingFiltersHint")}</p>
            </div>
            <div className="filter-control">
              <span className="filter-icon"><CalendarRange size={16} /></span>
              <input className="form-control" type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
              <span className="text-muted">—</span>
              <input className="form-control" type="date" value={to} onChange={(event) => setTo(event.target.value)} />
              <button className="btn btn-primary" type="submit"><Search size={16} /> {t(language, "apply")}</button>
              <button className="btn btn-secondary" type="button" onClick={resetFilter}>{t(language, "reset")}</button>
            </div>
          </form>
        </div>
      </div>

      {loading ? (
        <div className="card ops-card"><div className="card-body text-muted">{t(language, "loading")}...</div></div>
      ) : !overview ? (
        <div className="card ops-card"><div className="card-body text-muted">{t(language, "noData")}</div></div>
      ) : (
        <>
          <div className="ops-summary-grid">
            <SummaryTile icon={Euro} label={t(language, "revenueGross")} value={money(summary?.revenueGross)} hint={`${t(language, "revenueNet")}: ${money(summary?.revenueNet)}`} />
            <SummaryTile icon={Coins} label={t(language, "revenueTax")} value={money(summary?.revenueTax)} hint={`${t(language, "issuedInvoices")}: ${summary?.issuedInvoiceCount || 0}`} />
            <SummaryTile icon={ReceiptText} label={t(language, "expensesTotal")} value={money(summary?.expenseTotal)} hint={`${t(language, "expensesCount")}: ${summary?.expenseCount || 0}`} />
            <SummaryTile icon={BarChart3} label={t(language, "netResult")} value={money(summary?.netResult)} hint={`${t(language, "openReceivables")}: ${money(summary?.openReceivables)}`} />
          </div>

          <div className="row">
            <div className="col-xl-8">
              <div className="card ops-card">
                <div className="card-body">
                  <div className="settings-section-header">
                    <div>
                      <h4 className="card-title mb-1">{t(language, "revenueByMonth")}</h4>
                      <p className="text-muted mb-0">{t(language, "revenueByMonthHint")}</p>
                    </div>
                  </div>
                  {revenueByMonth.length === 0 ? <div className="text-muted mt-3">{t(language, "noData")}</div> : (
                    <div className="reporting-bar-chart">
                      {revenueByMonth.map((point) => (
                        <div key={point.period} className="reporting-bar-column">
                          <div className="reporting-bar-track">
                            <span className="reporting-bar-fill reporting-bar-net" style={{ height: `${Math.max(6, (Number(point.net || 0) / monthMax) * 100)}%` }} title={`${t(language, "revenueNet")}: ${money(point.net)}`}></span>
                            <span className="reporting-bar-fill reporting-bar-tax" style={{ height: `${Math.max(0, (Number(point.tax || 0) / monthMax) * 100)}%` }} title={`${t(language, "revenueTax")}: ${money(point.tax)}`}></span>
                          </div>
                          <small>{point.period}</small>
                          <strong>{money(point.gross)}</strong>
                        </div>
                      ))}
                    </div>
                  )}
                  <div className="chart-legend mt-3">
                    <span><i className="legend-dot bg-primary"></i>{t(language, "revenueNet")}</span>
                    <span><i className="legend-dot bg-warning"></i>{t(language, "revenueTax")}</span>
                  </div>
                </div>
              </div>

              <div className="card ops-card">
                <div className="card-body">
                  <div className="settings-section-header">
                    <div>
                      <h4 className="card-title mb-1">{t(language, "agingReceivables")}</h4>
                      <p className="text-muted mb-0">{t(language, "agingReceivablesHint")}</p>
                    </div>
                  </div>
                  {aging.length === 0 ? <div className="text-muted mt-3">{t(language, "noData")}</div> : (
                    <div className="table-responsive">
                      <table className="table table-centered table-nowrap ops-table mb-0">
                        <thead>
                          <tr>
                            <th>{t(language, "ageBucket")}</th>
                            <th>{t(language, "invoiceCount")}</th>
                            <th>{t(language, "openAmount")}</th>
                            <th>{t(language, "share")}</th>
                          </tr>
                        </thead>
                        <tbody>
                          {aging.map((bucket) => (
                            <tr key={bucket.bucket}>
                              <td><strong>{bucket.bucket} {t(language, "daysShort")}</strong></td>
                              <td>{bucket.invoiceCount}</td>
                              <td><strong>{money(bucket.openAmount)}</strong></td>
                              <td>
                                <div className="progress progress-sm">
                                  <div className="progress-bar bg-danger" style={{ width: `${(Number(bucket.openAmount || 0) / agingMax) * 100}%` }}></div>
                                </div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              </div>

              <div className="card ops-card">
                <div className="card-body">
                  <div className="settings-section-header">
                    <div>
                      <h4 className="card-title mb-1">{t(language, "expenseByCategory")}</h4>
                      <p className="text-muted mb-0">{t(language, "expenseByCategoryHint")}</p>
                    </div>
                  </div>
                  {expenseByCategory.length === 0 ? <div className="text-muted mt-3">{t(language, "noData")}</div> : (
                    <div className="table-responsive">
                      <table className="table table-centered table-nowrap ops-table mb-0">
                        <thead>
                          <tr>
                            <th>{t(language, "category")}</th>
                            <th>{t(language, "expensesCount")}</th>
                            <th>{t(language, "netAmount")}</th>
                            <th>{t(language, "taxAmount")}</th>
                            <th>{t(language, "totalAmount")}</th>
                            <th>{t(language, "share")}</th>
                          </tr>
                        </thead>
                        <tbody>
                          {expenseByCategory.map((row) => (
                            <tr key={row.category}>
                              <td><strong>{row.category}</strong></td>
                              <td>{row.count}</td>
                              <td>{money(row.netAmount)}</td>
                              <td>{money(row.taxAmount)}</td>
                              <td><strong>{money(row.totalAmount)}</strong></td>
                              <td>
                                <div className="progress progress-sm">
                                  <div className="progress-bar bg-info" style={{ width: `${(Number(row.totalAmount || 0) / expenseMax) * 100}%` }}></div>
                                </div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="col-xl-4">
              <div className="card ops-card">
                <div className="card-body">
                  <div className="form-panel-header">
                    <span className="form-panel-icon"><Coins size={18} /></span>
                    <div>
                      <h4 className="card-title mb-1">{t(language, "vatSummary")}</h4>
                      <p className="text-muted mb-0">{t(language, "vatSummaryHint")}</p>
                    </div>
                  </div>
                  {vatSummary.length === 0 ? <div className="text-muted">{t(language, "noData")}</div> : (
                    <div className="datev-mapping-list">
                      {vatSummary.map((bucket) => (
                        <div className="dunning-customer-row" key={bucket.ratePercent}>
                          <div className="entity-cell">
                            <span className="entity-avatar"><Coins size={18} /></span>
                            <span>
                              <strong>{Number(bucket.ratePercent || 0).toFixed(0)}% USt</strong>
                              <small>{t(language, "revenueNet")}: {money(bucket.net)}</small>
                            </span>
                          </div>
                          <span className="status-pill status-info">{money(bucket.tax)}</span>
                          <strong>{money(bucket.gross)}</strong>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              <div className="card ops-card">
                <div className="card-body">
                  <div className="form-panel-header">
                    <span className="form-panel-icon"><FileSpreadsheet size={18} /></span>
                    <div>
                      <h4 className="card-title mb-1">{t(language, "datevCoverage")}</h4>
                      <p className="text-muted mb-0">{t(language, "datevCoverageHint")}</p>
                    </div>
                  </div>
                  <CoverageRow
                    label={t(language, "invoices")}
                    exported={datevCoverage?.exportedInvoiceCount || 0}
                    total={datevCoverage?.invoiceCount || 0}
                    percent={datevCoverage?.coveragePercent || 0}
                  />
                  <CoverageRow
                    label={t(language, "expenses")}
                    exported={datevCoverage?.exportedExpenseCount || 0}
                    total={datevCoverage?.expenseCount || 0}
                    percent={datevCoverage?.expenseCoveragePercent || 0}
                  />
                </div>
              </div>

              <div className="card ops-card">
                <div className="card-body">
                  <div className="form-panel-header">
                    <span className="form-panel-icon"><Users size={18} /></span>
                    <div>
                      <h4 className="card-title mb-1">{t(language, "topCustomersByRevenue")}</h4>
                      <p className="text-muted mb-0">{t(language, "topCustomersByRevenueHint")}</p>
                    </div>
                  </div>
                  {topRevenueCustomers.length === 0 ? <div className="text-muted">{t(language, "noData")}</div> : (
                    <div className="datev-mapping-list">
                      {topRevenueCustomers.map((customer, index) => (
                        <div className="dunning-customer-row" key={`${customer.customerId || "anon"}-${index}`}>
                          <div className="entity-cell">
                            <span className="entity-avatar"><Users size={18} /></span>
                            <span>
                              <strong>{customer.customerName}</strong>
                              <small>{customer.invoiceCount} {t(language, "invoicesShort")}</small>
                            </span>
                          </div>
                          <strong>{money(customer.revenueGross)}</strong>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              <div className="card ops-card">
                <div className="card-body">
                  <div className="form-panel-header">
                    <span className="form-panel-icon"><Users size={18} /></span>
                    <div>
                      <h4 className="card-title mb-1">{t(language, "topCustomersByOverdue")}</h4>
                      <p className="text-muted mb-0">{t(language, "topCustomersByOverdueHint")}</p>
                    </div>
                  </div>
                  {topOverdueCustomers.length === 0 ? <div className="text-muted">{t(language, "noOverdueInvoices")}</div> : (
                    <div className="datev-mapping-list">
                      {topOverdueCustomers.map((customer, index) => (
                        <div className="dunning-customer-row" key={`${customer.customerId || "anon"}-${index}`}>
                          <div className="entity-cell">
                            <span className="entity-avatar"><Users size={18} /></span>
                            <span>
                              <strong>{customer.customerName}</strong>
                              <small>{customer.invoiceCount} {t(language, "overdueInvoices")}</small>
                            </span>
                          </div>
                          <strong className="text-danger">{money(customer.openAmount)}</strong>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </>
  );
}

function SummaryTile({ icon: Icon, label, value, hint }) {
  return (
    <div className="summary-tile">
      <span><Icon size={14} /> {label}</span>
      <strong>{value}</strong>
      {hint && <small className="text-muted d-block mt-1">{hint}</small>}
    </div>
  );
}

function CoverageRow({ label, exported, total, percent }) {
  return (
    <div className="progress-row">
      <div className="d-flex justify-content-between">
        <span>{label}</span>
        <strong>{exported}/{total} ({Number(percent || 0).toFixed(1)}%)</strong>
      </div>
      <div className="progress progress-sm">
        <div className="progress-bar bg-success" style={{ width: `${Math.min(100, Number(percent || 0))}%` }}></div>
      </div>
    </div>
  );
}

function money(value) {
  return `${Number(value || 0).toFixed(2)} EUR`;
}
