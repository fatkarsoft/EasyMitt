import { apiRequest } from "./client.js";

export const emailApi = {
  sendInvoice(id, body) {
    return apiRequest(`/api/v1/email/invoices/${id}/send`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
  },

  getInvoiceLogs(id) {
    return apiRequest(`/api/v1/email/invoices/${id}/logs`);
  },

  sendQuote(id, body) {
    return apiRequest(`/api/v1/email/quotes/${id}/send`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
  },

  getQuoteLogs(id) {
    return apiRequest(`/api/v1/email/quotes/${id}/logs`);
  },

  sendDunning(id, body) {
    return apiRequest(`/api/v1/email/dunning/${id}/send`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
  },

  getRecentLogs() {
    return apiRequest("/api/v1/email/logs");
  },
};
