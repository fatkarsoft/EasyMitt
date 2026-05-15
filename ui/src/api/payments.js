import { apiRequest, uploadRequest } from "./client.js";

function withFilters(path, query = "", status = "") {
  const params = new URLSearchParams();
  if (query) params.set("q", query);
  if (status) params.set("status", status);
  const suffix = params.toString();
  return suffix ? `${path}?${suffix}` : path;
}

export const paymentsApi = {
  list(query = "", status = "") {
    return apiRequest(withFilters("/api/v1/payments/transactions", query, status));
  },
  create(payload) {
    return apiRequest("/api/v1/payments/transactions", { method: "POST", body: JSON.stringify(payload) });
  },
  importCsv(file) {
    const formData = new FormData();
    formData.append("file", file);
    return uploadRequest("/api/v1/payments/transactions/import/csv", formData);
  },
  suggestions(transactionId) {
    return apiRequest(`/api/v1/payments/transactions/${transactionId}/suggestions`);
  },
  allocate(payload) {
    return apiRequest("/api/v1/payments/allocations", { method: "POST", body: JSON.stringify(payload) });
  },
  invoiceSummary(invoiceId) {
    return apiRequest(`/api/v1/payments/invoices/${invoiceId}/summary`);
  }
};
