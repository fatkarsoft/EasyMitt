import { useEffect, useState } from "react";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import PortalLayout from "../components/PortalLayout.js";
import { getPortalToken, portalApi } from "../api/portal.js";
import PortalEntry from "./PortalEntry.js";
import PortalDashboard from "./PortalDashboard.js";
import PortalInvoices from "./PortalInvoices.js";
import PortalInvoiceDetail from "./PortalInvoiceDetail.js";
import PortalQuotes from "./PortalQuotes.js";
import PortalQuoteDetail from "./PortalQuoteDetail.js";
import { t } from "../i18n.js";

const LANGUAGE_KEY = "easymitt.portal.language";

function readLanguage() {
  try {
    const value = sessionStorage.getItem(LANGUAGE_KEY);
    if (value === "tr" || value === "en" || value === "de") return value;
  } catch { /* noop */ }
  return navigator?.language?.startsWith("de") ? "de" : navigator?.language?.startsWith("tr") ? "tr" : "en";
}

export default function PortalShell() {
  const [session, setSession] = useState(null);
  const [loading, setLoading] = useState(() => !!getPortalToken());
  const [language, setLanguage] = useState(readLanguage);
  const location = useLocation();

  useEffect(() => {
    try { sessionStorage.setItem(LANGUAGE_KEY, language); } catch { /* noop */ }
  }, [language]);

  useEffect(() => {
    let alive = true;
    const token = getPortalToken();
    if (!token) return;
    portalApi.session()
      .then((data) => { if (alive) setSession(data); })
      .catch(() => { if (alive) setSession(null); })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, []);

  if (loading) {
    return (
      <div className="auth-shell">
        <div className="auth-card card">
          <div className="card-body text-center text-muted">{t(language, "loading")}...</div>
        </div>
      </div>
    );
  }

  if (!session) {
    if (location.pathname === "/portal" || location.pathname === "/portal/") {
      return <PortalEntry language={language} onSession={setSession} />;
    }
    return <Navigate to={`/portal${location.search}`} replace />;
  }

  return (
    <PortalLayout session={session} language={language} onLanguageChange={setLanguage}>
      <Routes>
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard" element={<PortalDashboard language={language} session={session} />} />
        <Route path="invoices" element={<PortalInvoices language={language} session={session} />} />
        <Route path="invoices/:id" element={<PortalInvoiceDetail language={language} session={session} />} />
        <Route path="quotes" element={<PortalQuotes language={language} session={session} />} />
        <Route path="quotes/:id" element={<PortalQuoteDetail language={language} session={session} />} />
        <Route path="*" element={<Navigate to="dashboard" replace />} />
      </Routes>
    </PortalLayout>
  );
}
