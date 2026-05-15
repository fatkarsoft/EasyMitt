# EasyMitt Scan Service

Local invoice/receipt image analysis service for EasyMitt.

It accepts JPEG, PNG, or WebP images and asks a local Ollama vision model to return EasyMitt-compatible raw invoice fields.

## Local Setup

```powershell
cd "C:\Github Projects\EasyMitt\scan-service"
npm install
npm run dev
```

Expected Ollama setup:

```powershell
ollama pull llama3.2-vision:11b
```

Environment variables:

- `PORT`: service port, default `7332`
- `OLLAMA_BASE_URL`: default `http://127.0.0.1:11434`
- `OLLAMA_VISION_MODEL`: default `llama3.2-vision:11b`
- `MAX_FILE_BYTES`: default `8388608`

Endpoints:

- `GET /health`
- `POST /api/scan/invoice` with multipart field `file`
