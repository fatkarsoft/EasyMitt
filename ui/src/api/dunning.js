import { apiRequest } from "./client.js";

export const dunningApi = {
  overview() {
    return apiRequest("/api/v1/dunning/overview");
  },
  invoiceReminders(invoiceId) {
    return apiRequest(`/api/v1/dunning/invoices/${invoiceId}/reminders`);
  },
  createReminder(payload) {
    return apiRequest("/api/v1/dunning/reminders", { method: "POST", body: JSON.stringify(payload) });
  }
};
