import { apiRequest } from "./client.js";

export const complianceApi = {
  dashboard({ from, to, status, riskLevel } = {}) {
    const params = new URLSearchParams();
    if (from) params.append("from", from);
    if (to) params.append("to", to);
    if (status) params.append("status", status);
    if (riskLevel) params.append("riskLevel", riskLevel);
    const qs = params.toString();
    return apiRequest(`/api/v1/compliance/dashboard${qs ? `?${qs}` : ""}`);
  },

  timeline(invoiceDraftId) {
    return apiRequest(`/api/v1/compliance/documents/${invoiceDraftId}/timeline`);
  },
};
