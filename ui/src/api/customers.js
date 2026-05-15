import { apiRequest } from "./client.js";

export const customersApi = {
  list(query = "", includeInactive = false) {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    if (includeInactive) params.set("includeInactive", "true");
    return apiRequest(`/api/v1/customers?${params.toString()}`);
  },
  get(id) {
    return apiRequest(`/api/v1/customers/${id}`);
  },
  create(payload) {
    return apiRequest("/api/v1/customers", { method: "POST", body: JSON.stringify(payload) });
  },
  update(id, payload) {
    return apiRequest(`/api/v1/customers/${id}`, { method: "PUT", body: JSON.stringify(payload) });
  },
  archive(id) {
    return apiRequest(`/api/v1/customers/${id}`, { method: "DELETE" });
  }
};
