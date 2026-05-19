import { useEffect, useState } from "react";
import { KeyRound } from "lucide-react";
import { useNavigate, useSearchParams } from "react-router-dom";
import iconLogo from "../assets/images/easymitt-icon.svg";
import { portalApi, setPortalToken } from "../api/portal.js";
import { ApiError } from "../api/client.js";
import { t } from "../i18n.js";

export default function PortalEntry({ language, onSession }) {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [token, setToken] = useState("");
  const [message, setMessage] = useState(null);
  const [working, setWorking] = useState(false);

  useEffect(() => {
    const queryToken = params.get("token");
    if (queryToken) {
      submit(queryToken);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function submit(value) {
    setWorking(true);
    setMessage(null);
    setPortalToken(value);
    try {
      const session = await portalApi.session();
      onSession(session);
      navigate("/portal/dashboard", { replace: true });
    } catch (err) {
      setPortalToken("");
      setMessage(err instanceof ApiError ? err.message : t(language, "portalInvalidToken"));
    } finally {
      setWorking(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card card">
        <div className="card-body">
          <div className="text-center mb-4">
            <img src={iconLogo} alt="EasyMitt" style={{ width: 48, height: 48 }} />
            <h4 className="mt-3 mb-1">{t(language, "portalTitle")}</h4>
            <p className="text-muted mb-0">{t(language, "portalSubtitle")}</p>
          </div>
          {message && <div className="alert alert-danger">{message}</div>}
          <form onSubmit={(event) => { event.preventDefault(); submit(token.trim()); }}>
            <div className="form-group mb-3">
              <label className="form-label">{t(language, "portalAccessToken")}</label>
              <div className="input-group">
                <span className="input-group-text"><KeyRound size={16} /></span>
                <input
                  className="form-control"
                  type="text"
                  autoFocus
                  placeholder={t(language, "portalAccessTokenHint")}
                  value={token}
                  onChange={(event) => setToken(event.target.value)}
                />
              </div>
            </div>
            <button className="btn btn-primary btn-block w-100" type="submit" disabled={working || !token.trim()}>
              {working ? `${t(language, "loading")}...` : t(language, "portalContinue")}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
