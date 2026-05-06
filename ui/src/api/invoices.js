import { apiRequest, downloadRequest } from "./client.js";

export const invoicesApi = {
  validate(document) {
    return apiRequest("/api/v1/invoices/validate", { method: "POST", body: JSON.stringify(document) });
  },
  saveDraft(document) {
    return apiRequest("/api/v1/invoices/drafts", { method: "POST", body: JSON.stringify(document) });
  },
  getDraft(id) {
    return apiRequest(`/api/v1/invoices/drafts/${id}`);
  },
  ingestRaw(payload) {
    return apiRequest("/api/v1/invoices/ingest/raw", { method: "POST", body: JSON.stringify(payload) });
  },
  exportXrechnung(document) {
    return downloadRequest("/api/v1/invoices/export/xrechnung", document, `${document.core["BT-1"] || "invoice"}.xml`);
  },
  exportZugferd(document) {
    return downloadRequest("/api/v1/invoices/export/zugferd-pdf", document, `${document.core["BT-1"] || "invoice"}.pdf`);
  },
  submitPeppol(document) {
    return apiRequest("/api/v1/invoices/peppol/submit", {
      method: "POST",
      body: JSON.stringify({ document, recipientEndpointId: document.buyer["BT-48"] })
    });
  }
};
