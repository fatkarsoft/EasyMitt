const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "";

export class ApiError extends Error {
  constructor(message, status, fieldErrors = {}, traceId = "") {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
    this.traceId = traceId;
  }
}

function getToken() {
  try {
    return JSON.parse(localStorage.getItem("easymitt.session") || "null")?.accessToken || "";
  } catch {
    return "";
  }
}

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

export async function apiRequest(path, options = {}) {
  const headers = new Headers(options.headers);
  headers.set("Content-Type", "application/json");
  const token = getToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });
  const contentType = response.headers.get("content-type") || "";
  if (!contentType.includes("application/json")) {
    if (!response.ok) throw new ApiError(response.statusText || "Request failed", response.status);
    return null;
  }
  return parseEnvelope(response);
}

export async function downloadRequest(path, body, fallbackName) {
  const headers = new Headers({ "Content-Type": "application/json" });
  const token = getToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "POST",
    headers,
    body: JSON.stringify(body)
  });

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
