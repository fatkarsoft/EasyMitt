import { useState } from "react";
import { ChevronDown, FileInput, FileText, Gauge, LogOut, Menu, PlusCircle, Settings } from "lucide-react";
import { Link, NavLink } from "react-router-dom";
import logoSm from "../assets/images/logo-sm.png";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const nav = [
  { to: "/dashboard", icon: Gauge, key: "dashboard" },
  { to: "/invoices", icon: FileText, key: "invoices" },
  { to: "/invoices/new", icon: PlusCircle, key: "newInvoice" },
  { to: "/invoices/raw", icon: FileInput, key: "rawImport" },
  { to: "/settings", icon: Settings, key: "settings" }
];

const languages = [
  { code: "tr", short: "TR" },
  { code: "en", short: "EN" },
  { code: "de", short: "DE" }
];

export default function Layout({ children }) {
  const [condensed, setCondensed] = useState(false);
  const [languageOpen, setLanguageOpen] = useState(false);
  const { user, language, setLanguage, logout } = useAuth();
  const currentLanguage = languages.find((item) => item.code === language) || languages[0];

  return (
    <div id="layout-wrapper" className={condensed ? "vertical-collpsed" : ""}>
      <header id="page-topbar">
        <div className="navbar-header">
          <div className="d-flex align-items-center">
            <div className="navbar-brand-box">
              <Link to="/dashboard" className="logo logo-light">
                <span className="logo-sm"><img src={logoSm} alt="EasyMitt" height="26" /></span>
                <span className="logo-lg easymitt-logo-text">EasyMitt</span>
              </Link>
            </div>
            <button className="btn btn-sm px-3 font-size-24 header-item waves-effect" onClick={() => setCondensed(!condensed)}>
              <Menu size={22} />
            </button>
            <div className="d-none d-md-block ml-3">
              <span className="topbar-title">{user?.companyName}</span>
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
                      setLanguage(item.code);
                      setLanguageOpen(false);
                    }}
                  >
                    <span className={`flag-mark flag-${item.code}`} aria-hidden="true"></span>
                    <span>{item.short}</span>
                  </button>
                ))}
              </div>
            </div>
            <span className={`badge badge-pill role-badge role-${String(user?.role || "").toLowerCase()} mx-2`}>{user?.role}</span>
            <button className="btn header-item waves-effect user-button" onClick={logout}>
              <span className="d-none d-xl-inline-block mr-1">{user?.displayName}</span>
              <LogOut size={17} />
            </button>
          </div>
        </div>
      </header>

      <div className="vertical-menu">
        <div className="h-100 sidebar-scroll">
          <div id="sidebar-menu">
            <ul className="metismenu list-unstyled" id="side-menu">
              <li className="menu-title">EasyMitt</li>
              {nav.map((item) => {
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
            <span>{new Date().getFullYear()} © EasyMitt</span>
          </div>
        </footer>
      </div>
    </div>
  );
}
