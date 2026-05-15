import { Download, Eye, FileSpreadsheet, Plus, RefreshCw, Trash2 } from "lucide-react";
import { Link } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";
import PageTitle from "../components/PageTitle.js";
import { ApiError } from "../api/client.js";
import { datevApi } from "../api/datev.js";
import { datevSettingsApi } from "../api/datevSettings.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const defaultDatevSettings = {
  exportFormat: "BasicCsv",
  chartOfAccounts: "SKR03",
  consultantNumber: "",
  clientNumber: "",
  fiscalYearStart: `${new Date().getFullYear()}-01-01`,
  revenueAccount: "8400",
  defaultExpenseAccount: "4980",
  customerContraAccount: "10000",
  vendorContraAccount: "70000",
  expenseAccountMappings: [
    { category: "Software", account: "4806" },
    { category: "Office", account: "4930" },
    { category: "Travel", account: "4660" }
  ],
  taxKeyMappings: [
    { source: "Invoice", vatRate: 19, taxKey: "3" },
    { source: "Invoice", vatRate: 7, taxKey: "2" },
    { source: "Invoice", vatRate: 0, taxKey: "" },
    { source: "Expense", vatRate: 19, taxKey: "9" },
    { source: "Expense", vatRate: 7, taxKey: "8" },
    { source: "Expense", vatRate: 0, taxKey: "" }
  ]
};

const today = new Date();
const defaultPeriod = {
  from: `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-01`,
  to: `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(new Date(today.getFullYear(), today.getMonth() + 1, 0).getDate()).padStart(2, "0")}`
};

export default function Datev() {
  const { language } = useAuth();
  const [datevSettings, setDatevSettings] = useState(defaultDatevSettings);
  const [exportLogs, setExportLogs] = useState([]);
  const [period, setPeriod] = useState(defaultPeriod);
  const [message, setMessage] = useState(null);
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let alive = true;
    async function load() {
      setLoading(true);
      setMessage(null);
      try {
        const [settings, logs] = await Promise.all([
          datevSettingsApi.get(),
          datevApi.listExports()
        ]);
        if (!alive) return;
        setDatevSettings({ ...defaultDatevSettings, ...settings });
        setExportLogs(logs || []);
      } catch (err) {
        if (alive) setMessage(["danger", err instanceof ApiError ? err.message : "DATEV could not be loaded."]);
      } finally {
        if (alive) setLoading(false);
      }
    }
    load();
    return () => { alive = false; };
  }, []);

  const summary = useMemo(() => {
    const logs = exportLogs || [];
    return {
      count: logs.length,
      warnings: logs.reduce((total, log) => total + Number(log.warningCount || 0), 0),
      rows: logs.reduce((total, log) => total + Number(log.rowCount || 0), 0),
      amount: logs.reduce((total, log) => total + Number(log.totalAmount || 0), 0)
    };
  }, [exportLogs]);

  function updateField(field, value) {
    setDatevSettings((current) => ({ ...current, [field]: value }));
  }

  function updateMapping(index, field, value) {
    setDatevSettings((current) => ({
      ...current,
      expenseAccountMappings: current.expenseAccountMappings.map((item, itemIndex) =>
        itemIndex === index ? { ...item, [field]: value } : item)
    }));
  }

  function addMapping() {
    setDatevSettings((current) => ({
      ...current,
      expenseAccountMappings: [...current.expenseAccountMappings, { category: "", account: "" }]
    }));
  }

  function removeMapping(index) {
    setDatevSettings((current) => ({
      ...current,
      expenseAccountMappings: current.expenseAccountMappings.filter((_, itemIndex) => itemIndex !== index)
    }));
  }

  function updateTaxKey(index, field, value) {
    setDatevSettings((current) => ({
      ...current,
      taxKeyMappings: current.taxKeyMappings.map((item, itemIndex) =>
        itemIndex === index ? { ...item, [field]: field === "vatRate" ? Number(value) : value } : item)
    }));
  }

  async function saveDatevSettings(event) {
    event.preventDefault();
    setSaving(true);
    setMessage(null);
    try {
      const saved = await datevSettingsApi.update(datevSettings);
      setDatevSettings({ ...defaultDatevSettings, ...saved });
      setMessage(["success", t(language, "datevSettingsSaved")]);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "DATEV settings could not be saved."]);
    } finally {
      setSaving(false);
    }
  }

  async function refreshHistory() {
    setMessage(null);
    try {
      setExportLogs(await datevApi.listExports());
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "DATEV export history could not be loaded."]);
    }
  }

  async function downloadExportLog(log) {
    setMessage(null);
    try {
      await datevApi.downloadExport(log.id, log.fileName || "datev-export.csv");
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "DATEV export could not be downloaded."]);
    }
  }

  function previewUrl(type) {
    const params = new URLSearchParams();
    params.set("type", type);
    if (period.from) params.set("from", period.from);
    if (period.to) params.set("to", period.to);
    return `/datev/preview?${params.toString()}`;
  }

  return (
    <>
      <PageTitle
        title={t(language, "datev")}
        action={<button className="btn btn-secondary" type="button" onClick={refreshHistory}><RefreshCw size={16} /> {t(language, "refresh")}</button>}
      />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="ops-summary-grid">
        <SummaryTile label={t(language, "datevExports")} value={summary.count} />
        <SummaryTile label={t(language, "datevWarnings")} value={summary.warnings} />
        <SummaryTile label={t(language, "datevRows")} value={summary.rows} />
        <SummaryTile label={t(language, "total")} value={summary.amount.toFixed(2)} />
      </div>

      <div className="card ops-card datev-period-card">
        <div className="card-body">
          <div className="settings-section-header">
            <div>
              <h4 className="card-title mb-1">{t(language, "datevCenter")}</h4>
              <p className="text-muted mb-0">{t(language, "datevCenterHint")}</p>
            </div>
          </div>
          <div className="balanced-form-grid mt-3">
            <Field span="span-3" label={t(language, "fromDate")}><input className="form-control" type="date" value={period.from} onChange={(event) => setPeriod({ ...period, from: event.target.value })} /></Field>
            <Field span="span-3" label={t(language, "toDate")}><input className="form-control" type="date" value={period.to} onChange={(event) => setPeriod({ ...period, to: event.target.value })} /></Field>
            <div className="balanced-field span-6 datev-action-strip">
              <Link className="btn btn-outline-primary" to={previewUrl("invoices")}><Eye size={16} /> {t(language, "datevInvoicePreview")}</Link>
              <Link className="btn btn-outline-primary" to={previewUrl("expenses")}><Eye size={16} /> {t(language, "datevExpensePreview")}</Link>
            </div>
          </div>
        </div>
      </div>

      <form className="card ops-card" onSubmit={saveDatevSettings}>
        <div className="card-body">
          <div className="settings-section-header">
            <div>
              <h4 className="card-title mb-1">{t(language, "datevSettings")}</h4>
              <p className="text-muted mb-0">{t(language, "datevSettingsHint")}</p>
            </div>
            <button className="btn btn-primary" type="submit" disabled={saving}>{saving ? `${t(language, "loading")}...` : t(language, "save")}</button>
          </div>
          <div className="form-grid form-grid-5 mt-4">
            <Field label={t(language, "datevExportFormat")}>
              <select className="form-control" value={datevSettings.exportFormat} onChange={(event) => updateField("exportFormat", event.target.value)}>
                <option value="BasicCsv">{t(language, "datevFormatBasicCsv")}</option>
                <option value="DatevExtf">{t(language, "datevFormatExtf")}</option>
              </select>
            </Field>
            <Field label={t(language, "chartOfAccounts")}>
              <select className="form-control" value={datevSettings.chartOfAccounts} onChange={(event) => updateField("chartOfAccounts", event.target.value)}>
                <option value="SKR03">SKR03</option>
                <option value="SKR04">SKR04</option>
              </select>
            </Field>
            <Field label={t(language, "datevConsultantNumber")}><input className="form-control" value={datevSettings.consultantNumber || ""} onChange={(event) => updateField("consultantNumber", event.target.value)} /></Field>
            <Field label={t(language, "datevClientNumber")}><input className="form-control" value={datevSettings.clientNumber || ""} onChange={(event) => updateField("clientNumber", event.target.value)} /></Field>
            <Field label={t(language, "fiscalYearStart")}><input className="form-control" type="date" value={datevSettings.fiscalYearStart || ""} onChange={(event) => updateField("fiscalYearStart", event.target.value)} /></Field>
            <Field label={t(language, "revenueAccount")}><input className="form-control" value={datevSettings.revenueAccount} onChange={(event) => updateField("revenueAccount", event.target.value)} /></Field>
            <Field label={t(language, "defaultExpenseAccount")}><input className="form-control" value={datevSettings.defaultExpenseAccount} onChange={(event) => updateField("defaultExpenseAccount", event.target.value)} /></Field>
            <Field label={t(language, "customerContraAccount")}><input className="form-control" value={datevSettings.customerContraAccount} onChange={(event) => updateField("customerContraAccount", event.target.value)} /></Field>
            <Field label={t(language, "vendorContraAccount")}><input className="form-control" value={datevSettings.vendorContraAccount} onChange={(event) => updateField("vendorContraAccount", event.target.value)} /></Field>
          </div>
          <div className="datev-mapping-header">
            <div>
              <h5>{t(language, "expenseAccountMappings")}</h5>
              <p className="text-muted mb-0">{t(language, "expenseAccountMappingsHint")}</p>
            </div>
            <button className="btn btn-outline-primary" type="button" onClick={addMapping}><Plus size={16} /> {t(language, "addMapping")}</button>
          </div>
          <div className="datev-mapping-list">
            {datevSettings.expenseAccountMappings.map((mapping, index) => (
              <div className="datev-mapping-row" key={`${mapping.category}-${index}`}>
                <Field label={t(language, "category")}><input className="form-control" value={mapping.category} onChange={(event) => updateMapping(index, "category", event.target.value)} placeholder="Software" /></Field>
                <Field label={t(language, "account")}><input className="form-control" value={mapping.account} onChange={(event) => updateMapping(index, "account", event.target.value)} placeholder="4806" /></Field>
                <button className="btn btn-icon btn-soft-danger datev-row-remove" type="button" onClick={() => removeMapping(index)} title={t(language, "delete")}><Trash2 size={15} /></button>
              </div>
            ))}
          </div>
          <div className="datev-mapping-header">
            <div>
              <h5>{t(language, "datevTaxKeys")}</h5>
              <p className="text-muted mb-0">{t(language, "datevTaxKeysHint")}</p>
            </div>
          </div>
          <div className="datev-tax-grid">
            {datevSettings.taxKeyMappings.map((mapping, index) => (
              <div className="datev-tax-row" key={`${mapping.source}-${mapping.vatRate}-${index}`}>
                <Field label={t(language, "source")}>
                  <select className="form-control" value={mapping.source} onChange={(event) => updateTaxKey(index, "source", event.target.value)}>
                    <option value="Invoice">{t(language, "invoices")}</option>
                    <option value="Expense">{t(language, "expenses")}</option>
                  </select>
                </Field>
                <Field label={t(language, "vatPercent")}><input className="form-control" type="number" step="0.01" value={mapping.vatRate} onChange={(event) => updateTaxKey(index, "vatRate", event.target.value)} /></Field>
                <Field label={t(language, "datevTaxKey")}><input className="form-control" value={mapping.taxKey || ""} onChange={(event) => updateTaxKey(index, "taxKey", event.target.value)} /></Field>
              </div>
            ))}
          </div>
        </div>
      </form>

      <div className="card ops-card mt-4">
        <div className="card-body">
          <div className="ops-toolbar">
            <div>
              <h4 className="card-title mb-1">{t(language, "datevExportHistory")}</h4>
              <p className="text-muted mb-0">{t(language, "datevExportHistoryHint")}</p>
            </div>
          </div>
          {loading ? <div className="text-muted">{t(language, "loading")}...</div> : exportLogs.length === 0 ? <div className="text-muted">{t(language, "noDatevExports")}</div> : (
            <div className="table-responsive">
              <table className="table table-centered table-nowrap table-hover ops-table mb-0">
                <thead>
                  <tr>
                    <th>{t(language, "type")}</th>
                    <th>{t(language, "period")}</th>
                    <th>{t(language, "createdAt")}</th>
                    <th>{t(language, "datevRows")}</th>
                    <th>{t(language, "datevWarnings")}</th>
                    <th>{t(language, "total")}</th>
                    <th>SHA-256</th>
                    <th>{t(language, "displayName")}</th>
                    <th className="text-right">{t(language, "actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {exportLogs.map((log) => (
                    <tr key={log.id}>
                      <td>
                        <div className="entity-cell">
                          <span className="entity-avatar avatar-product"><FileSpreadsheet size={18} /></span>
                          <span><strong>{log.exportType}</strong><small>{log.fileName}</small></span>
                        </div>
                      </td>
                      <td>{periodLabel(log.periodFrom, log.periodTo)}</td>
                      <td>{log.createdAtUtc ? new Date(log.createdAtUtc).toLocaleString() : "-"}</td>
                      <td>{log.rowCount}</td>
                      <td>{log.warningCount}</td>
                      <td>{Number(log.totalAmount || 0).toFixed(2)}</td>
                      <td><code>{shortHash(log.sha256Hex)}</code></td>
                      <td>{log.userDisplayName || log.userEmail || "-"}</td>
                      <td className="text-right">
                        <button className="btn btn-icon btn-soft-primary" type="button" disabled={!log.archiveObjectKey} onClick={() => downloadExportLog(log)} title={t(language, "download")}>
                          <Download size={15} />
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

function SummaryTile({ label, value }) {
  return <div className="summary-tile"><span>{label}</span><strong>{value}</strong></div>;
}

function Field({ label, span, children }) {
  return <div className={`balanced-field ${span || ""}`}><div className="form-group"><label>{label}</label>{children}</div></div>;
}

function shortHash(hash = "") {
  return hash.length > 16 ? `${hash.slice(0, 16)}...` : hash || "-";
}

function periodLabel(from, to) {
  if (!from && !to) return "-";
  return `${from || "..."} / ${to || "..."}`;
}
