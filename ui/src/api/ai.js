import { apiRequest } from "./client.js";

export const aiApi = {
  list({ suggestionType = "", status = "", targetType = "", targetId = "", take = 100 } = {}) {
    const params = new URLSearchParams();
    if (suggestionType) params.set("suggestionType", suggestionType);
    if (status) params.set("status", status);
    if (targetType) params.set("targetType", targetType);
    if (targetId) params.set("targetId", targetId);
    if (take) params.set("take", String(take));
    const qs = params.toString();
    return apiRequest(`/api/v1/ai/suggestions${qs ? `?${qs}` : ""}`);
  },
  get(id) {
    return apiRequest(`/api/v1/ai/suggestions/${id}`);
  },
  accept(id) {
    return apiRequest(`/api/v1/ai/suggestions/${id}/accept`, { method: "POST" });
  },
  reject(id) {
    return apiRequest(`/api/v1/ai/suggestions/${id}/reject`, { method: "POST" });
  },
  retry(id, snapshot = null) {
    return apiRequest(`/api/v1/ai/suggestions/${id}/retry`, {
      method: "POST",
      body: JSON.stringify(snapshot ? { snapshot } : {})
    });
  },
  log(payload, status = "Pending") {
    const url = `/api/v1/ai/suggestions/log${status ? `?status=${encodeURIComponent(status)}` : ""}`;
    return apiRequest(url, { method: "POST", body: JSON.stringify(payload) });
  },
  suggestCategory(body) {
    return apiRequest(`/api/v1/ai/category-suggest`, { method: "POST", body: JSON.stringify(body) });
  },
  suggestDatev(documentType, documentId) {
    return apiRequest(`/api/v1/ai/datev-suggest`, { method: "POST", body: JSON.stringify({ documentType, documentId }) });
  },
  suggestPaymentMatches(bankTransactionId) {
    return apiRequest(`/api/v1/ai/payment-suggest/${bankTransactionId}`);
  },
  suggestInvoiceFields(invoiceDraftId) {
    return apiRequest(`/api/v1/ai/invoice-field-suggest/${invoiceDraftId}`);
  }
};
