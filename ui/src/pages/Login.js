import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import brandLogo from "../assets/images/easymitt-brand.svg";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const demos = [
  ["admin@easymitt.local", "Admin123!", "Admin / TR"],
  ["accountant@easymitt.local", "Accountant123!", "Accountant / DE"],
  ["auditor@easymitt.local", "Auditor123!", "Auditor / EN"]
];

export default function Login() {
  const { login, language } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin@easymitt.local");
  const [password, setPassword] = useState("Admin123!");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function submit(event) {
    event.preventDefault();
    setLoading(true);
    setError("");
    try {
      await login(email, password);
      navigate("/dashboard", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Login failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <div className="home-btn d-none d-sm-block">
        <Link to="/login" className="text-dark"><i className="mdi mdi-home h2"></i></Link>
      </div>
      <div className="account-pages my-5 pt-5">
        <div className="container">
          <div className="row justify-content-center">
            <div className="col-md-8 col-lg-6 col-xl-5">
              <div className="card overflow-hidden">
                <div className="login-card-header">
                  <div className="text-center p-4">
                    <h5 className="text-white font-size-20">{t(language, "loginTitle")}</h5>
                    <p className="login-card-subtitle">{t(language, "loginSubtitle")}</p>
                    <span className="login-brand">
                      <img className="login-brand-logo" src={brandLogo} alt="EasyMitt" />
                    </span>
                  </div>
                </div>
                <div className="card-body p-4">
                  <div className="p-3">
                    {error && <div className="alert alert-danger">{error}</div>}
                    <form className="form-horizontal mt-3" onSubmit={submit}>
                      <div className="form-group">
                        <label>{t(language, "email")}</label>
                        <input className="form-control" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
                      </div>
                      <div className="form-group">
                        <label>{t(language, "password")}</label>
                        <input className="form-control" type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
                      </div>
                      <div className="form-group login-action mb-0">
                        <button className="btn btn-primary btn-login-submit waves-effect waves-light" disabled={loading}>
                          {loading ? `${t(language, "loading")}...` : t(language, "signIn")}
                        </button>
                      </div>
                    </form>
                    <div className="demo-users mt-4">
                      {demos.map(([demoEmail, demoPassword, label]) => (
                        <button key={demoEmail} className="btn btn-light btn-sm" onClick={() => { setEmail(demoEmail); setPassword(demoPassword); }}>
                          {label}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
              <div className="mt-5 text-center">
                <p className="mb-0">© {new Date().getFullYear()} EasyMitt</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
