import PageTitle from "../components/PageTitle.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

export default function Settings() {
  const { user, language, setLanguage, logout } = useAuth();

  return (
    <>
      <PageTitle title={t(language, "profile")} />
      <div className="card">
        <div className="card-body">
          <h4 className="card-title mb-4">{t(language, "profile")}</h4>
          <div className="table-responsive">
            <table className="table table-nowrap mb-4">
              <tbody>
                <Row label={t(language, "email")} value={user?.email} />
                <Row label={t(language, "displayName")} value={user?.displayName} />
                <Row label={t(language, "currentRole")} value={user?.role} />
                <Row label={t(language, "currentCompany")} value={user?.companyName} />
              </tbody>
            </table>
          </div>
          <div className="form-group max-320">
            <label>{t(language, "language")}</label>
            <select className="form-control" value={language} onChange={(e) => setLanguage(e.target.value)}>
              <option value="tr">{t(language, "turkish")}</option>
              <option value="en">{t(language, "english")}</option>
              <option value="de">{t(language, "german")}</option>
            </select>
          </div>
          <button className="btn btn-secondary" onClick={logout}>{t(language, "logout")}</button>
        </div>
      </div>
    </>
  );
}

function Row({ label, value }) {
  return <tr><th className="profile-label">{label}</th><td>{value || "-"}</td></tr>;
}
