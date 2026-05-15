import { apiRequest, uploadRequest } from "./client.js";

export const expensesApi = {
  list(query = "", status = "") {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    if (status) params.set("status", status);
    return apiRequest(`/api/v1/expenses?${params.toString()}`);
  },
  get(id) {
    return apiRequest(`/api/v1/expenses/${id}`);
  },
  create(payload) {
    return apiRequest("/api/v1/expenses", { method: "POST", body: JSON.stringify(payload) });
  },
  update(id, payload) {
    return apiRequest(`/api/v1/expenses/${id}`, { method: "PUT", body: JSON.stringify(payload) });
  },
  book(id) {
    return apiRequest(`/api/v1/expenses/${id}/book`, { method: "POST" });
  },
  archive(id) {
    return apiRequest(`/api/v1/expenses/${id}/archive`, { method: "POST" });
  },
  scan(file) {
    const formData = new FormData();
    formData.append("file", file);
    return uploadRequest("/api/v1/expenses/scan", formData);
  }
};
