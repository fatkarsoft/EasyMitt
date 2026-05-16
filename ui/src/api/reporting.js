import { apiRequest } from "./client.js";

export const reportingApi = {
  overview({ from, to } = {}) {
    const params = new URLSearchParams();
    if (from) params.set("from", from);
    if (to) params.set("to", to);
    const query = params.toString();
    return apiRequest(`/api/v1/reporting/overview${query ? `?${query}` : ""}`);
  }
};
