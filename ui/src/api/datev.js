import { apiRequest, downloadGetRequest } from "./client.js";

function withFilters(path, filters = {}) {
  const params = new URLSearchParams();
  if (filters.status) params.set("status", filters.status);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.force) params.set("force", "true");
  const query = params.toString();
  return query ? `${path}?${query}` : path;
}

export const datevApi = {
  listExports() {
    return apiRequest("/api/v1/datev/exports");
  },
  downloadExport(id, fallbackName = "datev-export.csv") {
    return downloadGetRequest(`/api/v1/datev/exports/${id}/download`, fallbackName);
  },
  previewInvoices(filters = {}) {
    return apiRequest(withFilters("/api/v1/datev/invoices/preview", typeof filters === "string" ? { status: filters } : filters));
  },
  previewExpenses(filters = {}) {
    return apiRequest(withFilters("/api/v1/datev/expenses/preview", typeof filters === "string" ? { status: filters } : filters));
  },
  exportInvoices(filters = {}) {
    return downloadGetRequest(withFilters("/api/v1/datev/invoices.csv", typeof filters === "string" ? { status: filters } : filters), "datev-invoices.csv");
  },
  exportExpenses(filters = {}) {
    return downloadGetRequest(withFilters("/api/v1/datev/expenses.csv", typeof filters === "string" ? { status: filters } : filters), "datev-expenses.csv");
  }
};
