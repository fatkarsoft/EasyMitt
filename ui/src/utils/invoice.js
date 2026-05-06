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
