import { apiRequest, downloadRequest, uploadRequest } from "./client.js";
import { prepareInvoiceDocument } from "../utils/invoice.js";

export const invoicesApi = {
  validate(document) {
    return apiRequest("/api/v1/invoices/validate", { method: "POST", body: JSON.stringify(prepareInvoiceDocument(document)) });
  },
  saveDraft(document, metadata = {}) {
    const prepared = prepareInvoiceDocument(document);
    return apiRequest("/api/v1/invoices/drafts", {
      method: "POST",
      body: JSON.stringify({
        document: prepared,
        customerId: metadata.customerId || null,
        productIds: (metadata.productIds || []).map((id) => id || null)
      })
    });
  },
  getDraft(id) {
    return apiRequest(`/api/v1/invoices/drafts/${id}`);
  },
  listDrafts(query = "", status = "") {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    if (status) params.set("status", status);
    return apiRequest(`/api/v1/invoices/drafts?${params.toString()}`);
  },
  issueDraft(id) {
    return apiRequest(`/api/v1/invoices/drafts/${id}/issue`, { method: "POST" });
  },
  sendDraft(id) {
    return apiRequest(`/api/v1/invoices/drafts/${id}/send`, { method: "POST" });
  },
  payDraft(id) {
    return apiRequest(`/api/v1/invoices/drafts/${id}/pay`, { method: "POST" });
  },
  markOverdue(id) {
    return apiRequest(`/api/v1/invoices/drafts/${id}/overdue`, { method: "POST" });
  },
  cancelDraft(id) {
    return apiRequest(`/api/v1/invoices/drafts/${id}/cancel`, { method: "POST" });
  },
  ingestRaw(payload) {
    return apiRequest("/api/v1/invoices/ingest/raw", { method: "POST", body: JSON.stringify(payload) });
  },
  ingestScan(file) {
    const formData = new FormData();
    formData.append("file", file);
    return uploadRequest("/api/v1/invoices/ingest/scan", formData);
  },
  exportXrechnung(document) {
    const prepared = prepareInvoiceDocument(document);
    return downloadRequest("/api/v1/invoices/export/xrechnung", prepared, `${prepared.core["BT-1"] || "invoice"}.xml`);
  },
  exportZugferd(document) {
    const prepared = prepareInvoiceDocument(document);
    return downloadRequest("/api/v1/invoices/export/zugferd-pdf", prepared, `${prepared.core["BT-1"] || "invoice"}.pdf`);
  },
  submitPeppol(document) {
    const prepared = prepareInvoiceDocument(document);
    return apiRequest("/api/v1/invoices/peppol/submit", {
      method: "POST",
      body: JSON.stringify({ document: prepared, recipientEndpointId: prepared.buyer["BT-48"] })
    });
  }
};
