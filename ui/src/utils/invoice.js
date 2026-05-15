const DRAFT_KEY = "easymitt.draftIds";

export function getDraftIds() {
  try {
    return JSON.parse(localStorage.getItem(DRAFT_KEY) || "[]").filter(Boolean);
  } catch {
    return [];
  }
}

export function rememberDraftId(id) {
  localStorage.setItem(DRAFT_KEY, JSON.stringify([id, ...getDraftIds().filter((item) => item !== id)]));
}

export function getDraftId(payload) {
  return payload?.id || payload?.draftId || "";
}

export function getDocument(record) {
  if (!record) return null;
  if (record.core) return record;
  return record.document || record.invoice || null;
}

export function emptyInvoice() {
  return {
    core: {
      "BT-1": "",
      "BT-2": new Date().toISOString().slice(0, 10),
      "BT-5": "EUR",
      "BT-10": "",
      "BT-110": 0,
      "BT-112": 0
    },
    seller: { "BT-20": "", "BT-22": "", "BT-34": "" },
    buyer: { "BT-26": "", "BT-48": null },
    lines: [{ "BT-126": "", "BT-129": 1, "BT-131": 0, "BT-151": 19 }]
  };
}

export function recalculate(document) {
  const tax = document.lines.reduce((sum, line) => sum + (Number(line["BT-131"] || 0) * Number(line["BT-151"] || 0)) / 100, 0);
  const net = document.lines.reduce((sum, line) => sum + Number(line["BT-131"] || 0), 0);
  return {
    ...document,
    core: {
      ...document.core,
      "BT-110": Number(tax.toFixed(2)),
      "BT-112": Number((net + tax).toFixed(2))
    }
  };
}

export function prepareInvoiceDocument(document) {
  const prepared = {
    ...document,
    core: {
      ...document.core,
      "BT-1": coalesce(document.core?.["BT-1"], document.core?.invoiceNumber),
      "BT-2": normalizeDate(coalesce(document.core?.["BT-2"], document.core?.issueDate)),
      "BT-5": coalesce(document.core?.["BT-5"], document.core?.currencyCode, "EUR"),
      "BT-10": coalesce(document.core?.["BT-10"], document.core?.buyerReference),
      "BT-110": Number(coalesce(document.core?.["BT-110"], document.core?.taxAmount, 0)),
      "BT-112": Number(coalesce(document.core?.["BT-112"], document.core?.invoiceTotalVatIncluded, 0))
    },
    seller: {
      ...document.seller,
      "BT-20": coalesce(document.seller?.["BT-20"], document.seller?.name),
      "BT-22": coalesce(document.seller?.["BT-22"], document.seller?.vatId),
      "BT-34": normalizeIban(coalesce(document.seller?.["BT-34"], document.seller?.paymentIban))
    },
    buyer: {
      ...document.buyer,
      "BT-26": coalesce(document.buyer?.["BT-26"], document.buyer?.name),
      "BT-48": coalesce(document.buyer?.["BT-48"], document.buyer?.vatId, null)
    },
    lines: document.lines.map((line) => ({
      ...line,
      "BT-126": coalesce(line["BT-126"], line.itemName),
      "BT-129": Number(coalesce(line["BT-129"], line.quantity, 1)),
      "BT-131": Number(coalesce(line["BT-131"], line.netAmount, 0)),
      "BT-151": Number(coalesce(line["BT-151"], line.vatRatePercent, 19))
    }))
  };

  return recalculate(prepared);
}

function coalesce(...values) {
  return values.find((value) => value !== undefined && value !== null && value !== "") ?? "";
}

function normalizeDate(value) {
  if (!value) return "";
  const text = String(value);
  if (/^\d{4}-\d{2}-\d{2}$/.test(text)) return text;
  const parts = text.match(/^(\d{2})[./](\d{2})[./](\d{4})$/);
  return parts ? `${parts[3]}-${parts[2]}-${parts[1]}` : text;
}

function normalizeIban(value) {
  return String(value || "").replace(/\s+/g, " ").trim();
}
