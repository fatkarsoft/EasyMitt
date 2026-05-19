import { useEffect, useState } from "react";
import { Play, RefreshCw } from "lucide-react";
import PageTitle from "../components/PageTitle.js";
import { ApiError } from "../api/client.js";
import { adminApi } from "../api/admin.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

function statusClass(status) {
  if (!status) return "status-muted";
  if (status === "Success") return "status-ready";
  if (status === "Failed") return "status-risk";
  return "status-info";
}

function formatDate(value) {
  if (!value) return "—";
  try {
    return new Date(value).toLocaleString();
  } catch {
    return value;
  }
}

export default function AdminJobs() {
  const { language, user } = useAuth();
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState("");
  const [message, setMessage] = useState(null);

  async function reload() {
    setLoading(true);
    setMessage(null);
    try {
      const data = await adminApi.listJobs();
      setJobs(Array.isArray(data) ? data : []);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let alive = true;
    adminApi.listJobs()
      .then((data) => { if (alive) setJobs(Array.isArray(data) ? data : []); })
      .catch((err) => { if (alive) setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]); })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, []);

  if (user?.role !== "Admin") {
    return (
      <>
        <PageTitle title={t(language, "adminJobs")} />
        <div className="alert alert-warning">{t(language, "adminOnly")}</div>
      </>
    );
  }

  async function run(name) {
    setBusy(name);
    setMessage(null);
    try {
      const data = await adminApi.runJob(name);
      setJobs((current) => current.map((j) => (j.name === data.name ? data : j)));
      setMessage(["success", t(language, "adminJobTriggered")]);
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Run failed"]);
    } finally {
      setBusy("");
    }
  }

  return (
    <>
      <PageTitle
        title={t(language, "adminJobs")}
        action={
          <button className="btn btn-secondary" type="button" onClick={reload}>
            <RefreshCw size={15} /> {t(language, "refresh")}
          </button>
        }
      />
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="card ops-card">
        <div className="card-body p-0">
          <div className="table-responsive">
            <table className="table table-borderless align-middle mb-0">
              <thead>
                <tr>
                  <th>{t(language, "adminJobName")}</th>
                  <th>{t(language, "adminJobDescription")}</th>
                  <th>{t(language, "adminJobSchedule")}</th>
                  <th>{t(language, "adminJobEnabled")}</th>
                  <th>{t(language, "adminJobLastRun")}</th>
                  <th>{t(language, "adminJobNextRun")}</th>
                  <th>{t(language, "adminJobLastStatus")}</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {loading && (
                  <tr><td colSpan={8} className="text-center text-muted py-4">{t(language, "loading") || "…"}</td></tr>
                )}
                {!loading && jobs.length === 0 && (
                  <tr><td colSpan={8} className="text-center text-muted py-4">{t(language, "noResults") || "—"}</td></tr>
                )}
                {!loading && jobs.map((job) => (
                  <tr key={job.name}>
                    <td><strong>{job.name}</strong></td>
                    <td className="text-muted">{job.description}</td>
                    <td><code>{job.schedule}</code></td>
                    <td>
                      <span className={`badge ${job.enabled ? "badge-soft-success" : "badge-soft-secondary"}`}>
                        {job.enabled ? t(language, "yes") : t(language, "no")}
                      </span>
                    </td>
                    <td>{formatDate(job.lastRunAtUtc)}</td>
                    <td>{formatDate(job.nextRunAtUtc)}</td>
                    <td>
                      <span className={`badge ${statusClass(job.lastStatus)}`}>
                        {job.lastStatus || "—"}
                      </span>
                      {job.lastError && <div className="text-danger small mt-1">{job.lastError}</div>}
                    </td>
                    <td className="text-end">
                      <button
                        className="btn btn-sm btn-primary"
                        type="button"
                        disabled={busy === job.name}
                        onClick={() => run(job.name)}
                      >
                        <Play size={14} /> {busy === job.name ? "…" : t(language, "adminJobRunNow")}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </>
  );
}
