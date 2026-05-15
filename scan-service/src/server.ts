import express from "express";
import multer from "multer";

type RawLineHint = {
  description: string | null;
  amount: number | null;
  vatRatePercent: number | null;
};

type RawInvoiceImport = {
  merchantOrSellerHint: string | null;
  buyerHint: string | null;
  ibanOrPaymentHint: string | null;
  sellerVatIdHint: string | null;
  buyerVatIdHint: string | null;
  buyerReferenceHint: string | null;
  totalAmount: number | null;
  currencyHint: string | null;
  issueDateHint: string | null;
  lineHints: RawLineHint[];
};

type ServiceError = {
  code: string;
  message: string;
};

const PORT = Number(process.env.PORT || 7332);
const OLLAMA_BASE_URL = trimTrailingSlash(process.env.OLLAMA_BASE_URL || "http://127.0.0.1:11434");
const OLLAMA_VISION_MODEL = process.env.OLLAMA_VISION_MODEL || "llama3.2-vision:11b";
const MAX_FILE_BYTES = Number(process.env.MAX_FILE_BYTES || 8 * 1024 * 1024);

const supportedMimeTypes = new Set(["image/jpeg", "image/jpg", "image/png", "image/webp"]);
const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: MAX_FILE_BYTES }
});

const app = express();
app.use(express.json({ limit: "1mb" }));

app.get("/health", async (_request, response) => {
  const ollama = await checkOllama();
  response.status(ollama.reachable ? 200 : 503).json({
    success: ollama.reachable,
    service: "easymitt-scan-service",
    provider: {
      type: "ollama",
      baseUrl: OLLAMA_BASE_URL,
      model: OLLAMA_VISION_MODEL
    },
    ollama
  });
});

app.post("/api/scan/invoice", upload.single("file"), async (request, response) => {
  const file = request.file;
  if (!file) {
    return response.status(400).json(failure("file_required", "A JPEG, PNG, or WebP invoice image is required."));
  }

  if (!supportedMimeTypes.has(file.mimetype)) {
    return response.status(400).json(failure("unsupported_file", "Only JPEG, PNG, and WebP images are supported."));
  }

  try {
    const raw = await analyzeWithOllama(file);
    return response.json({ success: true, data: raw, error: null });
  } catch (error) {
    const serviceError = toServiceError(error);
    const status = serviceError.code === "ollama_unavailable" || serviceError.code === "ollama_model_missing" ? 503 : 400;
    return response.status(status).json(failure(serviceError.code, serviceError.message));
  }
});

app.use((error: unknown, _request: express.Request, response: express.Response, _next: express.NextFunction) => {
  void _next;
  if (error instanceof multer.MulterError && error.code === "LIMIT_FILE_SIZE") {
    return response.status(400).json(failure("file_too_large", `File must be at most ${MAX_FILE_BYTES} bytes.`));
  }

  return response.status(500).json(failure("scan_service_error", error instanceof Error ? error.message : "Unexpected scan service error."));
});

app.listen(PORT, "127.0.0.1", () => {
  console.log(`EasyMitt scan-service listening on http://127.0.0.1:${PORT}`);
  console.log(`Using Ollama model ${OLLAMA_VISION_MODEL} at ${OLLAMA_BASE_URL}`);
});

async function analyzeWithOllama(file: Express.Multer.File): Promise<RawInvoiceImport> {
  const result = await fetch(`${OLLAMA_BASE_URL}/api/chat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      model: OLLAMA_VISION_MODEL,
      stream: false,
      format: "json",
      messages: [
        {
          role: "system",
          content:
            "You extract invoice and receipt data for EN16931 e-invoicing. Return only JSON. Use null for unknown fields. Prefer German VAT rates 0, 7, or 19."
        },
        {
          role: "user",
          content: buildPrompt(file.originalname),
          images: [file.buffer.toString("base64")]
        }
      ]
    })
  });

  const responseText = await result.text();
  if (!result.ok) {
    throw classifyOllamaError(result.status, responseText);
  }

  const completion = safeJsonParse<{ message?: { content?: string } }>(responseText);
  const content = completion?.message?.content;
  if (!content) {
    throw new ScanServiceException("empty_model_response", trimDetail(responseText));
  }

  const extracted = safeJsonParse<Partial<RawInvoiceImport>>(extractJsonObject(content));
  if (!extracted) {
    throw new ScanServiceException("invalid_model_json", trimDetail(content));
  }

  return normalizeRawInvoice(extracted);
}

function buildPrompt(fileName: string): string {
  return [
    `Analyze this invoice or receipt image (${fileName}).`,
    "Return JSON with this exact shape:",
    "{",
    "  \"merchantOrSellerHint\": string | null,",
    "  \"buyerHint\": string | null,",
    "  \"ibanOrPaymentHint\": string | null,",
    "  \"sellerVatIdHint\": string | null,",
    "  \"buyerVatIdHint\": string | null,",
    "  \"buyerReferenceHint\": string | null,",
    "  \"totalAmount\": number | null,",
    "  \"currencyHint\": \"EUR\" | string | null,",
    "  \"issueDateHint\": \"YYYY-MM-DD\" | null,",
    "  \"lineHints\": [",
    "    { \"description\": string | null, \"amount\": number | null, \"vatRatePercent\": 0 | 7 | 19 | null }",
    "  ]",
    "}",
    "Find the seller, buyer if visible, payment IBAN, VAT identifiers, total, date, currency, and item lines.",
    "Do not invent missing values."
  ].join("\n");
}

async function checkOllama(): Promise<{ reachable: boolean; modelAvailable: boolean; message: string }> {
  try {
    const response = await fetch(`${OLLAMA_BASE_URL}/api/tags`);
    if (!response.ok) {
      return { reachable: false, modelAvailable: false, message: `Ollama returned ${response.status}.` };
    }

    const payload = await response.json() as { models?: Array<{ name?: string }> };
    const modelAvailable = Boolean(payload.models?.some((model) => model.name === OLLAMA_VISION_MODEL));
    return {
      reachable: true,
      modelAvailable,
      message: modelAvailable ? "Ollama is reachable and model is available." : `Model ${OLLAMA_VISION_MODEL} is not installed.`
    };
  } catch (error) {
    return {
      reachable: false,
      modelAvailable: false,
      message: error instanceof Error ? error.message : "Ollama is not reachable."
    };
  }
}

function normalizeRawInvoice(input: Partial<RawInvoiceImport>): RawInvoiceImport {
  return {
    merchantOrSellerHint: asNullableString(input.merchantOrSellerHint),
    buyerHint: asNullableString(input.buyerHint),
    ibanOrPaymentHint: asNullableString(input.ibanOrPaymentHint),
    sellerVatIdHint: asNullableString(input.sellerVatIdHint),
    buyerVatIdHint: asNullableString(input.buyerVatIdHint),
    buyerReferenceHint: asNullableString(input.buyerReferenceHint),
    totalAmount: asNullableNumber(input.totalAmount),
    currencyHint: asNullableString(input.currencyHint) || "EUR",
    issueDateHint: asNullableDate(input.issueDateHint),
    lineHints: Array.isArray(input.lineHints) && input.lineHints.length > 0
      ? input.lineHints.map(normalizeLineHint)
      : [{ description: null, amount: asNullableNumber(input.totalAmount), vatRatePercent: 19 }]
  };
}

function normalizeLineHint(input: Partial<RawLineHint>): RawLineHint {
  return {
    description: asNullableString(input.description),
    amount: asNullableNumber(input.amount),
    vatRatePercent: normalizeVatRate(input.vatRatePercent)
  };
}

function normalizeVatRate(value: unknown): number | null {
  const numeric = asNullableNumber(value);
  if (numeric === null) return null;
  return [0, 7, 19].includes(numeric) ? numeric : 19;
}

function asNullableString(value: unknown): string | null {
  if (typeof value !== "string") return null;
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function asNullableDate(value: unknown): string | null {
  const text = asNullableString(value);
  return text && /^\d{4}-\d{2}-\d{2}$/.test(text) ? text : null;
}

function asNullableNumber(value: unknown): number | null {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  if (typeof value !== "string") return null;
  const normalized = value.replace(",", ".");
  const numeric = Number(normalized);
  return Number.isFinite(numeric) ? numeric : null;
}

function extractJsonObject(content: string): string {
  const trimmed = content.trim();
  if (trimmed.startsWith("{") && trimmed.endsWith("}")) return trimmed;

  const start = trimmed.indexOf("{");
  const end = trimmed.lastIndexOf("}");
  if (start >= 0 && end > start) return trimmed.slice(start, end + 1);

  throw new ScanServiceException("no_json_in_model_response", trimDetail(content));
}

function classifyOllamaError(status: number, responseText: string): ScanServiceException {
  if (status === 404 && responseText.toLowerCase().includes("model")) {
    return new ScanServiceException("ollama_model_missing", trimDetail(responseText));
  }

  if (status >= 500) {
    return new ScanServiceException("ollama_unavailable", trimDetail(responseText));
  }

  return new ScanServiceException(`ollama_failed_${status}`, trimDetail(responseText));
}

function failure(code: string, message: string): { success: false; data: null; error: ServiceError } {
  return { success: false, data: null, error: { code, message } };
}

function toServiceError(error: unknown): ServiceError {
  if (error instanceof ScanServiceException) {
    return { code: error.code, message: error.message };
  }

  if (error instanceof TypeError) {
    return { code: "ollama_unavailable", message: error.message };
  }

  return { code: "scan_failed", message: error instanceof Error ? error.message : "Scan analysis failed." };
}

function safeJsonParse<T>(value: string): T | null {
  try {
    return JSON.parse(value) as T;
  } catch {
    return null;
  }
}

function trimTrailingSlash(value: string): string {
  return value.endsWith("/") ? value.slice(0, -1) : value;
}

function trimDetail(value: string): string {
  return value.length <= 1200 ? value : value.slice(0, 1200);
}

class ScanServiceException extends Error {
  constructor(public readonly code: string, message: string) {
    super(message);
    this.name = "ScanServiceException";
  }
}
