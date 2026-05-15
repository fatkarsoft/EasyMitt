import { apiRequest } from "./client.js";
import { prepareInvoiceDocument } from "../utils/invoice.js";

export const quotesApi = {
  list(query = "", status = "") {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    if (status) params.set("status", status);
    return apiRequest(`/api/v1/quotes?${params.toString()}`);
  },
  get(id) {
    return apiRequest(`/api/v1/quotes/${id}`);
  },
  create(payload) {
    return apiRequest("/api/v1/quotes", { method: "POST", body: JSON.stringify(prepareQuotePayload(payload)) });
  },
  update(id, payload) {
    return apiRequest(`/api/v1/quotes/${id}`, { method: "PUT", body: JSON.stringify(prepareQuotePayload(payload)) });
  },
  send(id) {
    return apiRequest(`/api/v1/quotes/${id}/send`, { method: "POST" });
  },
  accept(id) {
    return apiRequest(`/api/v1/quotes/${id}/accept`, { method: "POST" });
  },
  decline(id) {
    return apiRequest(`/api/v1/quotes/${id}/decline`, { method: "POST" });
  },
  convertToInvoice(id) {
    return apiRequest(`/api/v1/quotes/${id}/convert-to-invoice`, { method: "POST" });
  }
};

function prepareQuotePayload(payload) {
  return {
    customerId: payload.customerId || null,
    productIds: (payload.productIds || []).map((id) => id || null),
    validUntilUtc: payload.validUntilUtc || null,
    document: prepareInvoiceDocument(payload.document)
  };
}
