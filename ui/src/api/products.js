import { apiRequest } from "./client.js";

export const productsApi = {
  list(query = "", includeInactive = false) {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    if (includeInactive) params.set("includeInactive", "true");
    return apiRequest(`/api/v1/products?${params.toString()}`);
  },
  get(id) {
    return apiRequest(`/api/v1/products/${id}`);
  },
  create(payload) {
    return apiRequest("/api/v1/products", { method: "POST", body: JSON.stringify(payload) });
  },
  update(id, payload) {
    return apiRequest(`/api/v1/products/${id}`, { method: "PUT", body: JSON.stringify(payload) });
  },
  archive(id) {
    return apiRequest(`/api/v1/products/${id}`, { method: "DELETE" });
  }
};
