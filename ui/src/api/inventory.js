import { apiRequest } from "./client.js";

export const inventoryApi = {
  movements(productId = "") {
    const params = new URLSearchParams();
    if (productId) params.set("productId", productId);
    return apiRequest(`/api/v1/inventory/movements?${params.toString()}`);
  },
  createMovement(payload) {
    return apiRequest("/api/v1/inventory/movements", { method: "POST", body: JSON.stringify(payload) });
  }
};
