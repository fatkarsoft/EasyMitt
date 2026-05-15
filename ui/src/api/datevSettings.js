import { apiRequest } from "./client.js";

export const datevSettingsApi = {
  get() {
    return apiRequest("/api/v1/datev/settings");
  },
  update(payload) {
    return apiRequest("/api/v1/datev/settings", {
      method: "PUT",
      body: JSON.stringify(payload)
    });
  }
};
