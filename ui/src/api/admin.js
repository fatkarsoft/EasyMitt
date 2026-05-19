import { apiRequest } from "./client.js";

export const adminApi = {
  listJobs() {
    return apiRequest("/api/v1/admin/jobs");
  },
  runJob(name) {
    return apiRequest(`/api/v1/admin/jobs/${encodeURIComponent(name)}/run`, { method: "POST" });
  },
};

export const complianceVerifyApi = {
  verifyArchive(invoiceId) {
    return apiRequest(`/api/v1/compliance/verify-archive/${invoiceId}`, { method: "POST" });
  },
};

export const invoiceSchematronApi = {
  validate(invoiceId) {
    return apiRequest(`/api/v1/invoices/${invoiceId}/validate-schematron`, { method: "POST" });
  },
};
