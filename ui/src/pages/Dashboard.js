import { Link } from "react-router-dom";
import { FileInput, FileText, PlusCircle } from "lucide-react";
import PageTitle from "../components/PageTitle.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";
import { getDraftIds } from "../utils/invoice.js";

export default function Dashboard() {
  const { user, language } = useAuth();
  const draftIds = getDraftIds();
  const copy = chartCopy[language] || chartCopy.en;
  const metrics = [
    [t(language, "totalDrafts"), draftIds.length],
    [t(language, "lastDraft"), draftIds[0] || "-"],
    [t(language, "currentRole"), user?.role || "-"],
    [t(language, "currentCompany"), user?.companyName || "-"]
  ];

  return (
    <>
      <PageTitle title={t(language, "dashboard")} />
      <div className="row">
        {metrics.map(([label, value]) => (
          <div className="col-xl-3 col-md-6" key={label}>
            <div className="card mini-stat">
              <div className="card-body">
                <div className="mini-stat-label">{label}</div>
                <h4 className="mt-2 mb-0 text-truncate">{value}</h4>
              </div>
            </div>
          </div>
        ))}
      </div>
      <div className="card">
        <div className="card-body">
          <h4 className="card-title mb-4">{t(language, "quickActions")}</h4>
          <div className="button-items">
            <Link to="/invoices/new" className="btn btn-primary"><PlusCircle size={16} /> {t(language, "newInvoice")}</Link>
            <Link to="/invoices/raw" className="btn btn-info"><FileInput size={16} /> {t(language, "rawImport")}</Link>
            <Link to="/invoices" className="btn btn-secondary"><FileText size={16} /> {t(language, "viewDrafts")}</Link>
          </div>
        </div>
      </div>
      <div className="row">
        <div className="col-xl-4">
          <div className="card chart-card">
            <div className="card-body">
              <h4 className="card-title mb-4">{copy.pipeline}</h4>
              <DonutChart total={Math.max(draftIds.length, 1)} draftCount={draftIds.length} />
              <div className="chart-legend mt-3">
                <span><i className="legend-dot bg-primary"></i>{copy.drafts}</span>
                <span><i className="legend-dot bg-success"></i>{copy.ready}</span>
                <span><i className="legend-dot bg-warning"></i>{copy.review}</span>
              </div>
            </div>
          </div>
        </div>
        <div className="col-xl-5">
          <div className="card chart-card">
            <div className="card-body">
              <h4 className="card-title mb-4">{copy.activity}</h4>
              <BarChart values={buildActivity(draftIds.length)} />
            </div>
          </div>
        </div>
        <div className="col-xl-3">
          <div className="card chart-card">
            <div className="card-body">
              <h4 className="card-title mb-4">{copy.compliance}</h4>
              <ProgressRow label="EN16931" value={draftIds.length > 0 ? 78 : 0} />
              <ProgressRow label="XRechnung" value={draftIds.length > 0 ? 64 : 0} />
              <ProgressRow label="Peppol" value={user?.role === "Auditor" ? 0 : 52} />
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

const chartCopy = {
  tr: {
    pipeline: "Fatura Pipeline",
    drafts: "Taslak",
    ready: "Hazır",
    review: "İnceleme",
    activity: "Haftalık İşlem Hacmi",
    compliance: "Uyumluluk Hazırlığı"
  },
  en: {
    pipeline: "Invoice Pipeline",
    drafts: "Drafts",
    ready: "Ready",
    review: "Review",
    activity: "Weekly Processing Volume",
    compliance: "Compliance Readiness"
  },
  de: {
    pipeline: "Rechnungspipeline",
    drafts: "Entwuerfe",
    ready: "Bereit",
    review: "Pruefung",
    activity: "Woechentliches Volumen",
    compliance: "Compliance-Bereitschaft"
  }
};

function buildActivity(count) {
  const base = Math.max(count, 1);
  return [
    { label: "Mon", value: base + 1 },
    { label: "Tue", value: base + 3 },
    { label: "Wed", value: base + 2 },
    { label: "Thu", value: base + 5 },
    { label: "Fri", value: base + 4 },
    { label: "Sat", value: Math.max(base - 1, 1) },
    { label: "Sun", value: base + 2 }
  ];
}

function DonutChart({ draftCount }) {
  const ready = draftCount > 0 ? Math.max(1, Math.round(draftCount * 0.35)) : 0;
  const review = draftCount > 0 ? Math.max(1, Math.round(draftCount * 0.2)) : 0;
  const draft = Math.max(draftCount, 1);
  const sum = draft + ready + review;
  const draftDash = `${(draft / sum) * 100} ${100 - (draft / sum) * 100}`;
  const readyDash = `${(ready / sum) * 100} ${100 - (ready / sum) * 100}`;
  const reviewDash = `${(review / sum) * 100} ${100 - (review / sum) * 100}`;

  return (
    <div className="donut-wrap">
      <svg className="donut-chart" viewBox="0 0 42 42">
        <circle className="donut-bg" cx="21" cy="21" r="15.915" />
        <circle className="donut-segment primary" cx="21" cy="21" r="15.915" strokeDasharray={draftDash} strokeDashoffset="25" />
        <circle className="donut-segment success" cx="21" cy="21" r="15.915" strokeDasharray={readyDash} strokeDashoffset={25 - (draft / sum) * 100} />
        <circle className="donut-segment warning" cx="21" cy="21" r="15.915" strokeDasharray={reviewDash} strokeDashoffset={25 - ((draft + ready) / sum) * 100} />
      </svg>
      <div className="donut-center">
        <strong>{draftCount}</strong>
        <span>total</span>
      </div>
    </div>
  );
}

function BarChart({ values }) {
  const max = Math.max(...values.map((item) => item.value));
  return (
    <div className="bar-chart">
      {values.map((item) => (
        <div className="bar-column" key={item.label}>
          <div className="bar-track">
            <span className="bar-fill" style={{ height: `${Math.max(12, (item.value / max) * 100)}%` }}></span>
          </div>
          <small>{item.label}</small>
        </div>
      ))}
    </div>
  );
}

function ProgressRow({ label, value }) {
  return (
    <div className="progress-row">
      <div className="d-flex justify-content-between">
        <span>{label}</span>
        <strong>{value}%</strong>
      </div>
      <div className="progress progress-sm">
        <div className="progress-bar" style={{ width: `${value}%` }}></div>
      </div>
    </div>
  );
}
