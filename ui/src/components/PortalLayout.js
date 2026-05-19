import { ChevronDown, FileSignature, FileText, Gauge, LogOut } from "lucide-react";
import { useState } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import iconLogo from "../assets/images/easymitt-icon.svg";
import { clearPortalToken } from "../api/portal.js";
import { t } from "../i18n.js";

const portalNav = [
  { to: "/portal/dashboard", icon: Gauge, key: "portalOverview" },
  { to: "/portal/invoices", icon: FileText, key: "portalInvoices" },
  { to: "/portal/quotes", icon: FileSignature, key: "portalQuotes" }
];

const languages = [
  { code: "tr", short: "TR" },
  { code: "en", short: "EN" },
  { code: "de", short: "DE" }
];

export default function PortalLayout({ children, session, language, onLanguageChange }) {
  const [languageOpen, setLanguageOpen] = useState(false);
  const navigate = useNavigate();
  const currentLanguage = languages.find((item) => item.code === language) || languages[0];

  const exit = () => {
    clearPortalToken();
    navigate("/portal", { replace: true });
  };

  return (
    <div id="layout-wrapper">
      <header id="page-topbar">
        <div className="navbar-header">
          <div className="d-flex align-items-center">
            <div className="navbar-brand-box">
              <Link to="/portal/dashboard" className="logo logo-light">
                <span className="logo-sm"><img className="easymitt-logo-icon" src={iconLogo} alt="EasyMitt" /></span>
                <span className="logo-lg sidebar-brand-lockup">
                  <img className="sidebar-brand-icon" src={iconLogo} alt="" aria-hidden="true" />
                  <span className="sidebar-brand-copy">
                    <strong>Easy<span>Mitt</span></strong>
                    <small>{t(language, "portalSubtitle")}</small>
                  </span>
                </span>
              </Link>
            </div>
            <div className="d-none d-md-block ml-3">
              <span className="topbar-title">{session?.companyName}</span>
            </div>
          </div>
          <div className="d-flex align-items-center">
            <div className="language-picker">
              <button className="btn header-language-button" type="button" onClick={() => setLanguageOpen(!languageOpen)}>
                <span className={`flag-mark flag-${currentLanguage.code}`} aria-hidden="true"></span>
                <span>{currentLanguage.short}</span>
                <ChevronDown size={14} />
              </button>
              <div className={`language-menu ${languageOpen ? "show" : ""}`}>
                {languages.map((item) => (
                  <button
                    className={`language-option ${item.code === language ? "active" : ""}`}
                    key={item.code}
                    type="button"
                    onClick={() => {
                      onLanguageChange(item.code);
                      setLanguageOpen(false);
                    }}
                  >
                    <span className={`flag-mark flag-${item.code}`} aria-hidden="true"></span>
                    <span>{item.short}</span>
                  </button>
                ))}
              </div>
            </div>
            <span className="badge badge-pill role-badge role-portal mx-2">{t(language, "portalRoleBadge")}</span>
            <button className="btn header-item waves-effect user-button" type="button" onClick={exit}>
              <span className="d-none d-xl-inline-block mr-1">{session?.customerDisplayName}</span>
              <LogOut size={17} />
            </button>
          </div>
        </div>
      </header>

      <div className="vertical-menu">
        <div className="h-100 sidebar-scroll">
          <div id="sidebar-menu">
            <ul className="metismenu list-unstyled" id="side-menu">
              <li className="menu-title">{t(language, "portal")}</li>
              {portalNav.map((item) => {
                const Icon = item.icon;
                return (
                  <li key={item.to}>
                    <NavLink to={item.to} className={({ isActive }) => `waves-effect ${isActive ? "mm-active" : ""}`}>
                      <Icon className="side-icon" size={18} />
                      <span>{t(language, item.key)}</span>
                    </NavLink>
                  </li>
                );
              })}
            </ul>
          </div>
        </div>
      </div>

      <div className="main-content">
        <div className="page-content">
          <div className="container-fluid">{children}</div>
        </div>
        <footer className="footer">
          <div className="container-fluid">
            <span>{new Date().getFullYear()} © EasyMitt — {t(language, "portalSubtitle")}</span>
          </div>
        </footer>
      </div>
    </div>
  );
}
