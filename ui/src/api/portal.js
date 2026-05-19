import { ApiError } from "./client.js";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "";
const PORTAL_TOKEN_KEY = "easymitt.portal.token";

function fieldErrors(errors) {
  if (!errors) return {};
  return Object.fromEntries(Object.entries(errors).map(([key, value]) => [key, value.map((item) => item.message)]));
}

async function parseEnvelope(response) {
  const payload = await response.json();
  if (!response.ok || payload.success === false) {
    throw new ApiError(payload.message || response.statusText || "Request failed", response.status, fieldErrors(payload.errors), payload.traceId);
  }
  return payload.data;
}

export function getPortalToken() {
  try {
    return sessionStorage.getItem(PORTAL_TOKEN_KEY) || "";
  } catch {
    return "";
  }
}

export function setPortalToken(token) {
  if (token) {
    sessionStorage.setItem(PORTAL_TOKEN_KEY, token);
  } else {
    sessionStorage.removeItem(PORTAL_TOKEN_KEY);
  }
}

export function clearPortalToken() {
  sessionStorage.removeItem(PORTAL_TOKEN_KEY);
}

async function portalRequest(path, options = {}) {
  const headers = new Headers(options.headers);
  headers.set("Content-Type", "application/json");
  const token = getPortalToken();
  if (token) headers.set("X-Portal-Token", token);
  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });
  return parseEnvelope(response);
}

async function portalDownload(path, fallbackName) {
  const token = getPortalToken();
  const headers = new Headers();
  if (token) headers.set("X-Portal-Token", token);
  const response = await fetch(`${API_BASE_URL}${path}`, { method: "GET", headers });
  const contentType = response.headers.get("content-type") || "";
  if (!response.ok || contentType.includes("application/json")) {
    await parseEnvelope(response);
    return;
  }
  const blob = await response.blob();
  const disposition = response.headers.get("content-disposition") || "";
  const match = disposition.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
  const fileName = match ? decodeURIComponent(match[1]) : fallbackName;
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}

export const portalApi = {
  session() {
    return portalRequest("/api/v1/portal/me");
  },
  listInvoices() {
    return portalRequest("/api/v1/portal/invoices");
  },
  getInvoice(id) {
    return portalRequest(`/api/v1/portal/invoices/${id}`);
  },
  downloadInvoicePdf(id, fallbackName) {
    return portalDownload(`/api/v1/portal/invoices/${id}/zugferd.pdf`, fallbackName || `Rechnung-${id}.pdf`);
  },
  downloadInvoiceXml(id, fallbackName) {
    return portalDownload(`/api/v1/portal/invoices/${id}/xrechnung.xml`, fallbackName || `Rechnung-${id}.xml`);
  },
  listQuotes() {
    return portalRequest("/api/v1/portal/quotes");
  },
  getQuote(id) {
    return portalRequest(`/api/v1/portal/quotes/${id}`);
  },
  acceptQuote(id) {
    return portalRequest(`/api/v1/portal/quotes/${id}/accept`, { method: "POST" });
  },
  declineQuote(id) {
    return portalRequest(`/api/v1/portal/quotes/${id}/decline`, { method: "POST" });
  }
};

export const portalAccessApi = {
  list(customerId) {
    return apiRequestAuth(`/api/v1/customers/${customerId}/portal-access`);
  },
  issue(customerId, payload) {
    return apiRequestAuth(`/api/v1/customers/${customerId}/portal-access`, {
      method: "POST",
      body: JSON.stringify(payload || {})
    });
  },
  revoke(tokenId) {
    return apiRequestAuth(`/api/v1/customers/portal-access/${tokenId}/revoke`, { method: "POST" });
  }
};

function authHeader() {
  try {
    const t = JSON.parse(localStorage.getItem("easymitt.session") || "null")?.accessToken;
    return t ? `Bearer ${t}` : "";
  } catch {
    return "";
  }
}

async function apiRequestAuth(path, options = {}) {
  const headers = new Headers(options.headers);
  headers.set("Content-Type", "application/json");
  const bearer = authHeader();
  if (bearer) headers.set("Authorization", bearer);
  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });
  return parseEnvelope(response);
}
