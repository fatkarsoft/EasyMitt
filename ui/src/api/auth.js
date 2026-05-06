import { apiRequest } from "./client.js";

export const authApi = {
  login(email, password) {
    return apiRequest("/api/v1/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password })
    });
  },
  me() {
    return apiRequest("/api/v1/auth/me");
  }
};
