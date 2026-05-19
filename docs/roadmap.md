# EasyMitt Roadmap

Last updated: 2026-05-19 (Production Hardening)

## Product North Star

EasyMitt is a Germany-focused SaaS platform for e-invoicing and accounting operations. The intended product shape is:

- simpler and easier than sevDesk for day-to-day invoice work
- stronger than a basic invoicing tool for German compliance
- AI-assisted for import, classification, matching, and review
- focused on Germany only: XRechnung, ZUGFeRD, DATEV, GoBD, Leitweg-ID, German VAT, Mahnwesen

## Completed

### Foundation

- Clean Architecture backend with .NET 10.
- React/Vite frontend with JavaScript-only components.
- Common response envelope.
- Global exception handling.
- Localization in `tr`, `en`, `de`.
- Scalar/OpenAPI docs.
- Role-based auth and authorization.
- PostgreSQL persistence.
- Tenant-scoped SaaS foundation.

### Germany E-Invoicing

- EN16931 BT-code DTO mapping.
- XRechnung export.
- ZUGFeRD PDF export.
- German VAT policy.
- IBAN normalization and validation policy.
- Leitweg-ID fields.

### AI Import

- Local scan service foundation.
- Ollama vision model integration path.
- Scan import API adapter.
- Raw import UI retained.

### SaaS Business Core

- Companies.
- Users.
- Customers with B2B/B2C handling.
- Product/service catalog.
- Inventory movements.
- Invoice draft/customer/product selection flow.

### Sales And Billing

- Invoice list, create, detail.
- Invoice lifecycle.
- Quotes.
- Quote-to-invoice conversion.
- Expenses.
- Bank payments.
- Payment matching/allocation.
- Partial payment status.
- Mahnwesen / dunning.

### Accounting And Compliance Foundation

- Immutable archive foundation.
- DATEV settings.
- DATEV preview.
- DATEV export history.
- Tax key mappings.
- Export locking/force re-export path.

### Compliance Center

- Central compliance dashboard at `/compliance`.
- XRechnung, ZUGFeRD, GoBD, DATEV readiness progress cards.
- Document risk list: missing BT fields, not GoBD archived, not DATEV exported, overdue without reminders.
- Audit timeline per invoice: status transitions, archive, and dunning events.
- Date range, status, and risk level filters.
- All roles read. No new migration.

### Email Delivery

- Send invoice/quote/reminder emails with ZUGFeRD PDF attachment for invoices.
- `email_delivery_logs` table: status (Sent/Failed), toEmail, subject, attachment type, sender.
- SMTP via `System.Net.Mail` with NoOp fallback when SmtpHost is empty.
- `SendEmailModal` component used in InvoiceDetail, QuoteDetail, and Dunning.
- i18n keys for TR/EN/DE.
- Admin/Accountant write, Auditor read-only.

### Reporting Dashboard

- `/api/v1/reporting/overview` endpoint with date range filters.
- Revenue by month, VAT summary (0% / 7% / 19%), DATEV export coverage.
- Overdue receivables with aging buckets (0-30 / 31-60 / 61-90 / 90+).
- Top customers by revenue and by overdue amount.
- Expense summary by category.
- `/reporting` page with charts and KPI cards.
- UTC-safe date handling (backend `DateTime.SpecifyKind(..., Utc)`, frontend `toLocalIso`).
- No new DB migration. Reads from existing tables.

### Customer Portal

- Per-customer portal access tokens in `customer_portal_access` (SHA-256 hashed, status Active/Revoked, expiry, last-used).
- Admin-side issue/revoke UI inside Customer edit (plaintext token shown once + sharable `/portal?token=` link).
- Public portal endpoints under `/api/v1/portal/*` (no JWT — `X-Portal-Token` header or `?token=` query): `/me`, `/invoices`, `/invoices/{id}`, `/invoices/{id}/zugferd.pdf`, `/invoices/{id}/xrechnung.xml`, `/quotes`, `/quotes/{id}`, `/quotes/{id}/accept`, `/quotes/{id}/decline`.
- Portal frontend with its own layout (no staff sidebar): overview KPIs, invoice list/detail (PDF + XML download), quote list/detail (accept/decline).
- Tenant + customer scoping enforced server-side; Draft invoices and Draft quotes are hidden from the portal.
- TR/EN/DE i18n.
- Migration `20260516184328_CustomerPortalAccess` applied.

### Advanced AI Accounting

- `ai_suggestions` audit table (Pending / Accepted / Rejected / Superseded) with target type+id and JSON payload.
- Backend heuristics in `EasyMitt.Domain.Accounting` (pure functions, unit-testable without DB):
  `ExpenseCategoryHeuristics`, `PaymentMatchScorer`, `DatevAccountHeuristics`, `InvoiceFieldHeuristics`.
- Application abstractions `IExpenseCategorySuggester`, `IDatevAccountSuggester`, `IPaymentMatchScorer`, `IMissingFieldSuggester`, `IAiSuggestionRepository`.
- Infrastructure impls in `EasyMitt.Infrastructure.Ai.*` plus `AiSuggestionRepository`.
- Endpoints under `/api/v1/ai`: `POST /datev-suggest`, `POST /category-suggest`, `GET /payment-suggest/{txId}`, `GET /invoice-field-suggest/{invoiceId}`, `GET /suggestions`, `GET/POST /suggestions/{id}` accept|reject|retry, `POST /suggestions/log`.
- Receipt scan flow attaches a category suggestion (heuristic on vendor + line text + amount).
- Bank-tx → invoice scorer with weighted confidence (amount-exact 0.6 / IBAN 0.2 / name 0.1 / date 0.1) and auto pre-selection at confidence ≥ 0.85.
- Compliance Center risk list shows "Suggested fix" column (e.g. Buyer VAT looks like Leitweg-ID → BT-10).
- Reusable `<AiSuggestionPill />` component in TR / EN / DE used on ExpenseForm, InvoiceDetail, Payments page, and Compliance.
- `/ai` activity panel (sidebar entry) shows recent suggestions with type+status filters; Admin/Accountant can re-trigger Rejected (creates new Pending + supersedes old).
- Migration `20260519101616_AiSuggestions` applied.

### Production Hardening

- Secrets pulled from environment / `appsettings.Development.json`; `appsettings.json` keeps non-secret defaults only. `EASYMITT__Section__Key` env override supported. Startup logs a warning when `Authentication:SigningKey` is missing; `/health/ready` flags it via `SecretsHealthCheck`.
- Archive pluggable behind `IImmutableArchiveStore`: `LocalFileImmutableArchiveStore` (default) + `S3ObjectLockArchiveStore` (AWS SDK, Object Lock COMPLIANCE mode, SHA-256 PUT). `Archive:Backend = Local | S3`. `POST /api/v1/compliance/verify-archive/{invoiceId}` (Admin-only) re-reads bytes and compares SHA-256 against `ArchiveObjectKey`.
- Schematron pipeline: `XRechnungSchematronRules` (Domain, pure) evaluates a curated KoSIT subset (BR-02..BR-07, BR-16, BR-CO-15, BR-DE-01, BR-DE-15, BR-DE-16, BR-DE-21, BR-DE-23, BR-DE-26). `POST /api/v1/invoices/{id}/validate-schematron` returns failures; Compliance Center surfaces them as `schematron_*` risk codes + new readiness card + table column.
- Background jobs via Quartz.NET: `EmailRetryJob` (every 15 min, 3 retries with exp backoff for Failed logs in last 24h), `OverdueInvoiceJob` (daily, transitions `Issued`/`Sent` to `Overdue` by BT-9), `DatevExportScheduledJob` (opt-in cron). `GET /api/v1/admin/jobs` + `POST /api/v1/admin/jobs/{name}/run` (Admin-only) list/trigger.
- Observability: Serilog request logging with `TraceId` / `CompanyId` / `UserId` enrichment (JSON in production, console template in development). OpenTelemetry traces + metrics with ASP.NET Core, HttpClient, EF Core instrumentation, OTLP exporter (no-op when `Telemetry:OtlpEndpoint` is empty). `/health/live` (process up) and `/health/ready` (db + archive + secrets) endpoints alongside the existing `/health`.
- Production Peppol Access Point: `PartnerGatewayInvoiceDispatch` (HTTP POST to a partner gateway) behind `Dispatch:Backend = NoOp | PartnerGateway`. New `dispatch_logs` table (id, company_id, invoice_id, backend, status, partner_id, response_json, created_at_utc), `IDispatchLogRepository`. `PeppolSubmitRequestDto.InvoiceId` enables persistence; Compliance Center adds a "Dispatched" readiness card + column + audit timeline event.
- Production email provider: `PostmarkEmailService` (Postmark HTTP API). `Email:Backend = NoOp | Smtp | Postmark`. NoOp/Smtp paths preserved.
- New `AuthorizationPolicies.AdminOnly` policy for admin job + verify endpoints.
- Frontend: `/admin/jobs` page (Admin-only sidebar entry) with last-run/next-run/last-status + Run-now; Compliance Center adds Schematron + Dispatched cards/columns; InvoiceDetail adds "Validate Schematron" and "Verify archive" buttons. ~25 new i18n keys for TR / EN / DE.
- Migration `20260519144130_DispatchLogs` applied.

## Next Major Module

### LLM-Backed AI Suggestions

Goal: Replace the heuristic `IExpenseCategorySuggester` and field-fix `IMissingFieldSuggester` with Ollama-vision-backed implementations behind the same interfaces, with the existing heuristic kept as fallback.

- New `OllamaExpenseCategorySuggester` and `OllamaMissingFieldSuggester` Infrastructure adapters wrapping the local scan service or `/api/generate` Ollama endpoint.
- `Ai:Backend = Heuristic | Ollama` config + DI selector. Heuristic remains the fallback when Ollama is unreachable.
- Track which adapter produced each `ai_suggestions` row (`payload.source = "heuristic" | "ollama"`).
- Latency budget + per-suggestion confidence calibration.

## Upcoming Modules

### Multi-Tenant Operations

- Per-company admin settings page (logos, tax IDs, default DATEV mapping).
- User management page (invite, deactivate, change role).
- Audit log surface for sensitive admin actions.

## Backlog

- PDF upload support for scan service.
- Multi-page scan analysis.
- Reminder PDF/letter generation.
- Mahngebühren / interest rules if legally and product-wise desired.
- Bank API integration via PSD2 provider, after CSV import is stable.
- DATEV Unternehmen Online style export packaging.
- Admin company settings page.
- User management page.
