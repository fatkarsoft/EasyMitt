import { useEffect, useState } from "react";
import { ArrowLeft, Building2, ClipboardCopy, Globe2, KeyRound, ShieldOff } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import PageTitle from "../components/PageTitle.js";
import FormField from "../components/FormField.js";
import { customersApi } from "../api/customers.js";
import { portalAccessApi } from "../api/portal.js";
import { ApiError } from "../api/client.js";
import { useAuth } from "../state/auth.js";
import { t } from "../i18n.js";

const emptyCustomer = {
  type: "Business",
  companyName: "",
  firstName: "",
  lastName: "",
  email: "",
  phone: "",
  street: "",
  postalCode: "",
  city: "",
  countryCode: "DE",
  vatId: "",
  taxNumber: "",
  leitwegId: "",
  datevDebitorAccount: "",
  paymentTermsDays: 14,
  notes: "",
  isActive: true
};

export default function CustomerForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { language, canWrite } = useAuth();
  const [form, setForm] = useState(emptyCustomer);
  const [message, setMessage] = useState(null);
  const [loading, setLoading] = useState(!!id);
  const isEdit = !!id;
  const isBusiness = form.type === "Business";

  useEffect(() => {
    if (!id) return;
    let alive = true;
    customersApi.get(id)
      .then((data) => alive && setForm({ ...emptyCustomer, ...data }))
      .catch((err) => alive && setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, [id]);

  async function submit(event) {
    event.preventDefault();
    setMessage(null);
    const payload = normalizeByType(form);
    try {
      if (isEdit) {
        await customersApi.update(id, payload);
      } else {
        await customersApi.create(payload);
      }
      navigate("/customers");
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    }
  }

  return (
    <>
      <PageTitle
        title={isEdit ? t(language, "editCustomer") : t(language, "newCustomer")}
        action={<Link className="btn btn-secondary" to="/customers"><ArrowLeft size={16} /> {t(language, "backToList")}</Link>}
      />
      {!canWrite && <div className="alert alert-warning">{t(language, "readonly")}</div>}
      {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
      <div className="row">
        <div className="col-12">
          <form className="card ops-card" onSubmit={submit}>
            <div className="card-body">
              <div className="form-panel-header">
                <span className="form-panel-icon"><Building2 size={18} /></span>
                <div>
                  <h4 className="card-title mb-1">{isEdit ? t(language, "editCustomer") : t(language, "newCustomer")}</h4>
                  <p className="text-muted mb-0">{t(language, "customerFormHint")}</p>
                </div>
              </div>
              {loading ? <div className="text-muted">{t(language, "loading")}...</div> : (
                <>
                  <div className="balanced-form-grid">
                    <Field span="span-3" label={t(language, "type")}><select className="form-control" value={form.type} onChange={(e) => setForm(switchCustomerType(form, e.target.value))}><option value="Business">{t(language, "b2bOption")}</option><option value="Consumer">{t(language, "b2cOption")}</option></select></Field>
                    {isBusiness ? (
                      <Field span="span-9" label={t(language, "companyName")}><input className="form-control" value={form.companyName || ""} onChange={(e) => set(form, setForm, "companyName", e.target.value)} /></Field>
                    ) : (
                      <>
                        <Field span="span-4" label={t(language, "firstName")}><input className="form-control" value={form.firstName || ""} onChange={(e) => set(form, setForm, "firstName", e.target.value)} /></Field>
                        <Field span="span-5" label={t(language, "lastName")}><input className="form-control" value={form.lastName || ""} onChange={(e) => set(form, setForm, "lastName", e.target.value)} /></Field>
                      </>
                    )}
                    <Field span="span-6" label={t(language, "email")}><input className="form-control" value={form.email || ""} onChange={(e) => set(form, setForm, "email", e.target.value)} /></Field>
                    <Field span="span-6" label={t(language, "phone")}><input className="form-control" value={form.phone || ""} onChange={(e) => set(form, setForm, "phone", e.target.value)} /></Field>
                    <Field span="span-12" label={t(language, "street")}><input className="form-control" value={form.street || ""} onChange={(e) => set(form, setForm, "street", e.target.value)} /></Field>
                    <Field span="span-3" label={t(language, "postalCode")}><input className="form-control" value={form.postalCode || ""} onChange={(e) => set(form, setForm, "postalCode", e.target.value)} /></Field>
                    <Field span="span-5" label={t(language, "city")}><input className="form-control" value={form.city || ""} onChange={(e) => set(form, setForm, "city", e.target.value)} /></Field>
                    <Field span="span-4" label={t(language, "paymentTermsDays")}><input className="form-control" type="number" value={form.paymentTermsDays} onChange={(e) => set(form, setForm, "paymentTermsDays", Number(e.target.value))} /></Field>
                    <Field span="span-4" label={t(language, "datevDebitorAccount")}><input className="form-control" value={form.datevDebitorAccount || ""} onChange={(e) => set(form, setForm, "datevDebitorAccount", e.target.value)} placeholder="10001" /></Field>
                    {isBusiness && (
                      <>
                        <Field span="span-4" label={t(language, "vatId")}><input className="form-control" value={form.vatId || ""} onChange={(e) => set(form, setForm, "vatId", e.target.value)} /></Field>
                        <Field span="span-4" label={t(language, "taxNumber")}><input className="form-control" value={form.taxNumber || ""} onChange={(e) => set(form, setForm, "taxNumber", e.target.value)} /></Field>
                        <Field span="span-4" label={t(language, "leitwegId")}><input className="form-control" value={form.leitwegId || ""} onChange={(e) => set(form, setForm, "leitwegId", e.target.value)} /></Field>
                      </>
                    )}
                    <Field span="span-12" label={t(language, "notes")}><textarea className="form-control" rows="4" value={form.notes || ""} onChange={(e) => set(form, setForm, "notes", e.target.value)} /></Field>
                  </div>
                  <div className="button-items mt-3">
                    <button className="btn btn-primary" disabled={!canWrite}>{t(language, "save")}</button>
                    <Link className="btn btn-secondary" to="/customers">{t(language, "cancel")}</Link>
                  </div>
                </>
              )}
            </div>
          </form>
          {isEdit && <PortalAccessPanel customerId={id} language={language} canWrite={canWrite} />}
        </div>
      </div>
    </>
  );
}

function PortalAccessPanel({ customerId, language, canWrite }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);
  const [issued, setIssued] = useState(null);
  const [label, setLabel] = useState("");
  const [validityDays, setValidityDays] = useState(180);
  const [working, setWorking] = useState(false);

  const load = () => {
    setLoading(true);
    portalAccessApi.list(customerId)
      .then((data) => setItems(Array.isArray(data) ? data : []))
      .catch((err) => setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    let alive = true;
    portalAccessApi.list(customerId)
      .then((data) => { if (alive) setItems(Array.isArray(data) ? data : []); })
      .catch((err) => { if (alive) setMessage(["danger", err instanceof ApiError ? err.message : "Load failed"]); })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, [customerId]);

  async function issue(event) {
    event.preventDefault();
    setWorking(true);
    setMessage(null);
    try {
      const data = await portalAccessApi.issue(customerId, {
        label: label.trim() || null,
        validityDays: Number(validityDays) || null
      });
      setIssued(data);
      setMessage(["success", t(language, "portalTokenIssued")]);
      setLabel("");
      load();
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    } finally {
      setWorking(false);
    }
  }

  async function revoke(tokenId) {
    if (!confirm(t(language, "portalRevokeConfirm"))) return;
    setMessage(null);
    try {
      await portalAccessApi.revoke(tokenId);
      setMessage(["success", t(language, "portalTokenRevoked")]);
      load();
    } catch (err) {
      setMessage(["danger", err instanceof ApiError ? err.message : "Request failed"]);
    }
  }

  function copy(text) {
    try { navigator.clipboard.writeText(text); } catch { /* noop */ }
  }

  return (
    <div className="card ops-card mt-3">
      <div className="card-body">
        <div className="form-panel-header">
          <span className="form-panel-icon"><Globe2 size={18} /></span>
          <div>
            <h4 className="card-title mb-1">{t(language, "portalAccessTitle")}</h4>
            <p className="text-muted mb-0">{t(language, "portalAccessHint")}</p>
          </div>
        </div>
        {message && <div className={`alert alert-${message[0]}`}>{message[1]}</div>}
        {issued && (
          <div className="alert alert-info">
            <strong>{t(language, "portalAccessOnceWarning")}</strong>
            <div className="mt-2 d-flex align-items-center" style={{ gap: 8, flexWrap: "wrap" }}>
              <code style={{ wordBreak: "break-all" }}>{issued.token}</code>
              <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => copy(issued.token)}><ClipboardCopy size={14} /> {t(language, "portalCopyToken")}</button>
            </div>
            {issued.portalUrl && (
              <div className="mt-2 d-flex align-items-center" style={{ gap: 8, flexWrap: "wrap" }}>
                <span className="text-muted small">{t(language, "portalShareLink")}:</span>
                <code style={{ wordBreak: "break-all" }}>{issued.portalUrl}</code>
                <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => copy(issued.portalUrl)}><ClipboardCopy size={14} /> {t(language, "portalCopyLink")}</button>
              </div>
            )}
          </div>
        )}
        {canWrite && (
          <form className="row g-2 align-items-end mb-3" onSubmit={issue}>
            <div className="col-md-6">
              <FormField label={t(language, "portalAccessLabel")}>
                <input className="form-control" value={label} onChange={(e) => setLabel(e.target.value)} placeholder={t(language, "portalAccessLabelPlaceholder")} />
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label={t(language, "portalValidityDays")}>
                <input className="form-control" type="number" min="0" value={validityDays} onChange={(e) => setValidityDays(e.target.value)} />
              </FormField>
            </div>
            <div className="col-md-3">
              <button className="btn btn-primary w-100" type="submit" disabled={working}>
                <KeyRound size={16} /> {t(language, "portalIssueAccess")}
              </button>
            </div>
          </form>
        )}
        {loading ? <div className="text-muted">{t(language, "loading")}...</div> : items.length === 0 ? (
          <div className="text-muted">{t(language, "portalNoTokens")}</div>
        ) : (
          <div className="table-responsive">
            <table className="table table-sm align-middle mb-0">
              <thead><tr>
                <th>{t(language, "portalAccessLabel")}</th>
                <th>{t(language, "portalAccessPrefix")}</th>
                <th>{t(language, "portalStatus")}</th>
                <th>{t(language, "portalCreatedAt")}</th>
                <th>{t(language, "portalExpiresAt")}</th>
                <th>{t(language, "portalLastUsed")}</th>
                <th className="text-end">{t(language, "actions")}</th>
              </tr></thead>
              <tbody>
                {items.map((token) => (
                  <tr key={token.id}>
                    <td>{token.label}</td>
                    <td><code>{token.tokenPrefix}…</code></td>
                    <td>
                      <span className={`status-pill ${token.status === "Active" ? "status-issued" : "status-cancelled"}`}>
                        {token.status === "Active" ? t(language, "portalStatusActive") : t(language, "portalStatusRevoked")}
                      </span>
                    </td>
                    <td>{new Date(token.createdAtUtc).toLocaleString()}</td>
                    <td>{token.expiresAtUtc ? new Date(token.expiresAtUtc).toLocaleDateString() : "—"}</td>
                    <td>{token.lastUsedAtUtc ? new Date(token.lastUsedAtUtc).toLocaleString() : "—"}</td>
                    <td className="text-end">
                      {canWrite && token.status === "Active" && (
                        <button className="btn btn-sm btn-outline-danger" type="button" onClick={() => revoke(token.id)}>
                          <ShieldOff size={14} /> {t(language, "portalRevoke")}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

function Field({ label, span = "span-6", children }) {
  return <div className={`balanced-field ${span}`}><FormField label={label}>{children}</FormField></div>;
}

function set(form, setForm, key, value) {
  setForm({ ...form, [key]: value });
}

function switchCustomerType(form, type) {
  if (type === "Consumer") {
    return {
      ...form,
      type,
      companyName: "",
      vatId: "",
      taxNumber: "",
      leitwegId: ""
    };
  }

  return {
    ...form,
    type: "Business",
    firstName: "",
    lastName: ""
  };
}

function normalizeByType(form) {
  return switchCustomerType(form, form.type);
}
