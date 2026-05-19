# EasyMitt Agent Handoff

Last updated: 2026-05-19 (Production Hardening)

## Current Goal

EasyMitt is being evolved into a Germany-only SaaS platform for e-invoicing, accounting operations, and compliance. The product direction is: sevDesk-level ease of use plus Sovos-like compliance strength for German workflows.

The immediate development style requested by the user is to complete large modules in one coherent pass instead of splitting them into many small partial improvements.

## Latest Completed Work

The latest completed module is `Production Hardening`:

- Secret hygiene: `appsettings.json` now holds only non-secret defaults. Sensitive values (PostgreSQL connection, `Authentication:SigningKey`, `Email:Smtp*`, AWS / Postmark / Peppol partner tokens) live in `appsettings.Development.json` for local dev or `EASYMITT__Section__Key` environment variables in production. Added `AddEnvironmentVariables(prefix: "EASYMITT__")` in `Program.cs`. Startup warns when `Authentication:SigningKey` is missing; `/health/ready` `SecretsHealthCheck` flags it as Degraded.
- Pluggable immutable archive (`IImmutableArchiveStore`): existing `LocalFileImmutableArchiveStore` retained; new `S3ObjectLockArchiveStore` writes with Object Lock COMPLIANCE mode + retention days via AWSSDK.S3. Selector via `Archive:Backend = Local | S3`. New `POST /api/v1/compliance/verify-archive/{invoiceId}` (Admin-only) re-reads archive bytes, re-computes SHA-256, and compares against `InvoiceDraftEntity.CanonicalSha256Hex`/`ArchiveObjectKey`.
- Schematron pipeline: `EasyMitt.Domain.Compliance.XRechnungSchematronRules` (pure functions) evaluates a curated KoSIT subset — `BR-02`, `BR-03`, `BR-05`, `BR-06`, `BR-07`, `BR-16`, `BR-CO-15`, `BR-DE-01`, `BR-DE-15`, `BR-DE-16`, `BR-DE-21`, `BR-DE-23`, `BR-DE-26`. `InvoiceSchematronValidator` adapter in Infrastructure. New `POST /api/v1/invoices/{id}/validate-schematron` endpoint. Compliance dashboard returns per-document `isSchematronValid` + `schematronFailureCodes`; risks now include `schematron_*` codes.
- Background job runtime via **Quartz.NET** (in-memory store, cron-scheduled): `EmailRetryJob` (every 15 min — retries last 24h Failed `email_delivery_logs` 3× with exp backoff), `OverdueInvoiceJob` (daily 04:00 UTC — transitions Issued/Sent → Overdue by `BT-9` or BT-2 + 14d), `DatevExportScheduledJob` (opt-in cron). `JobRegistry` + `JobRunHistory` track last-run/last-status/last-error. New `GET /api/v1/admin/jobs` and `POST /api/v1/admin/jobs/{name}/run` (Admin-only).
- Observability stack: **Serilog** request logging with `TraceId`, `CompanyId`, `UserId` enrichment (`UseSerilogRequestLogging`); console template in dev, `RenderedCompactJsonFormatter` in production. **OpenTelemetry** traces + metrics with ASP.NET Core / HttpClient / EF Core instrumentation, OTLP exporter activated only when `Telemetry:OtlpEndpoint` is set (no-op otherwise). Health endpoints: `/health` (legacy), `/health/live` (process up), `/health/ready` (db + archive + secrets). All return the common envelope.
- Production Peppol AP adapter: `PartnerGatewayInvoiceDispatch` (HTTP POST with `Bearer` API key + `{senderId, recipientId, contentType, payloadBase64}` body) behind `Dispatch:Backend = NoOp | PartnerGateway`. `NoOpInvoiceDispatch` retained. New `dispatch_logs` table (`id`, `company_id`, `invoice_id`, `backend`, `status`, `partner_id`, `response_json`, `created_at_utc`) + `IDispatchLogRepository`. `PeppolSubmitRequestDto.InvoiceId` enables persistence on submit. Compliance Center gets a new "Dispatched" readiness card + table column + "dispatched" audit timeline event.
- Production email provider adapter: `PostmarkEmailService` (Postmark HTTP API; one-way send + status — no webhook). `Email:Backend = NoOp | Smtp | Postmark` selector. `SmtpEmailService` and `NoOpEmailService` preserved as before.
- Authorization: new `AuthorizationPolicies.AdminOnly` policy. All admin endpoints (`/api/v1/admin/jobs*`, `/api/v1/compliance/verify-archive/*`) require Admin role.
- DI overhaul (`EasyMitt.Infrastructure.DependencyInjection`): backend selectors for Email / Archive / Dispatch, `IArchiveVerifier`, `IInvoiceSchematronValidator`, Quartz registration, health checks. Health-check classes registered Scoped (not Singleton) to legally consume `EasyMittDbContext`.
- New `MessageKeys`: `JobsFound`, `JobTriggered`, `JobNotFound`, `SchematronValid`, `SchematronInvalid`, `ArchiveVerified`, `ArchiveHashMismatch`, `ArchiveNotFound`, `SystemReady`, `SystemNotReady`. Localized in EN / TR / DE.
- Migration `20260519144130_DispatchLogs` applied.
- Frontend: new `/admin/jobs` page (Admin-only sidebar entry with `Cpu` icon) listing all jobs with description, cron schedule, enabled flag, last-run timestamp, next-fire time, last status badge, last error, Run-now button. Compliance Center adds Schematron + Dispatched readiness cards and table columns. InvoiceDetail adds "Validate Schematron" and "Verify archive" buttons; results render inline with hashes/rule failures. New `ui/src/api/admin.js` (`adminApi`, `invoiceSchematronApi`, `complianceVerifyApi`). ~25 new i18n keys for TR / EN / DE.

Latest validation known to pass:

```powershell
dotnet build .\service\EasyMitt.slnx
# Build succeeded. 0 Warning(s). 0 Error(s).
cd ui
npm run lint
# clean
npm run build
# Built in ~8.7s.
```

Live end-to-end smoke test (admin@easymitt.local / Admin123!):

- `GET /health/ready` → 200, all 3 checks (database / archive / secrets) Healthy.
- `GET /health/live` → 200, process up.
- `GET /api/v1/admin/jobs` → envelope, 3 jobs returned (`email-retry`, `overdue-invoice`, `datev-export-scheduled`) with cron + next-run times.
- `POST /api/v1/admin/jobs/overdue-invoice/run` → envelope, `lastRunAtUtc` populated, `lastStatus=Success`.
- `POST /api/v1/invoices/{id}/validate-schematron` against an Issued invoice → envelope, `isValid:true` with informational `BR-DE-26` only.
- `POST /api/v1/compliance/verify-archive/{id}` against an archived invoice → envelope, `hashMatches:true`, `expected` and `actual` SHA-256 match.
- `EASYMITT__Authentication__SigningKey=test123-override-env-prefix` env override → API starts, `/health/ready` reports secrets Healthy, login succeeds.
- API on 5095 + UI on 5173 verified up.

The previous completed module is `Advanced AI Accounting`:

- Migration `20260519101616_AiSuggestions` applied. New table `ai_suggestions` with id, company_id, suggestion_type (`ExpenseCategory` | `DatevAccount` | `PaymentMatch` | `InvoiceField`), target_type, target_id, payload_json, status (`Pending` | `Accepted` | `Rejected` | `Superseded`), created_at_utc, decided_at_utc, decided_by_user_email, superseded_by_id.
- Domain (pure functions, no DB — unit-testable): `EasyMitt.Domain.Accounting.ExpenseCategoryHeuristics`, `PaymentMatchScorer`, `DatevAccountHeuristics`, `InvoiceFieldHeuristics`.
- Application abstractions: `IExpenseCategorySuggester`, `IDatevAccountSuggester`, `IPaymentMatchScorer`, `IMissingFieldSuggester`, `IAiSuggestionRepository`. New DTOs in `EasyMitt.Application.Dtos.Ai`.
- Infrastructure impls in `EasyMitt.Infrastructure.Ai.*`: `ExpenseCategorySuggester`, `DatevAccountSuggester` (uses `IDatevSettingsRepository` tax-key + expense-account mappings as source of truth), `PaymentMatchScorerService`, `MissingFieldSuggester`. `AiSuggestionRepository` in `Persistence/Repositories/`.
- Endpoints under `/api/v1/ai`:
  - `POST /datev-suggest` (body `{documentType, documentId}` → account + confidence + taxKey + matchedRule)
  - `POST /category-suggest` (body `{vendorName, lineDescriptions, totalAmount, currencyCode}`)
  - `GET /payment-suggest/{bankTransactionId}` (returns confidence + autoPreselect for top match)
  - `GET /invoice-field-suggest/{invoiceDraftId}`
  - `GET /suggestions` (filters: suggestionType, status, targetType, targetId, take)
  - `GET /suggestions/{id}`
  - `POST /suggestions/{id}/accept` | `/reject` | `/retry` (retry creates new Pending + supersedes old)
  - `POST /suggestions/log?status=...` for client-side accept/reject logging.
- Authorization: read = `InvoiceRead` (Admin/Accountant/Auditor); accept/reject/retry = `InvoiceWrite` (Admin/Accountant).
- Scan endpoint (`POST /api/v1/expenses/scan`) now attaches a `suggestion` object (category + confidence + rationale) to the response and pre-fills the expense category when confidence ≥ 0.6.
- Bank-tx scorer uses weighted blend (amount-exact 0.6 / IBAN 0.2 / name fuzzy 0.1 / date proximity 0.1) and only auto-pre-selects the top suggestion at confidence ≥ 0.85.
- DI: `IConfigureWarnings` ignores `RelationalEventId.PendingModelChangesWarning` in DbContext setup to avoid runtime-only false-positive vs CLI snapshot diff.
- New `MessageKeys` for AI flows localized for EN / TR / DE (`AiSuggestionsFound`, `AiSuggestionAccepted`, `AiSuggestionRejected`, `AiSuggestionRetried`, `AiSuggestionNotFound`, `AiSuggestionNotPending`, `AiCategorySuggested`, `AiDatevAccountSuggested`, `AiPaymentMatchSuggested`, `AiInvoiceFieldsSuggested`, `AiDocumentNotFound`).
- Frontend: `ui/src/components/AiSuggestionPill.js` reusable pill (label + value + confidence% + rationale tooltip + accept/reject; status `Accepted` / `Rejected` is displayed instead of buttons after decision).
- Frontend: `ui/src/api/ai.js`.
- Frontend integration:
  - `ExpenseForm.js`: category suggestion pill above the Category field (auto-computed from vendor/notes/amount with 250ms debounce, and from scan response); DATEV credit-account pill above the DATEV field (loaded on edit page).
  - `InvoiceDetail.js`: DATEV revenue-account pill + a "Suggested fix" list rendering field-correction suggestions per invoice.
  - `Payments.js`: replaces the legacy suggestions list with the AI scorer endpoint, renders a colored confidence progress bar per row, pre-selects top suggestion when confidence ≥ 0.85, and logs the accepted match into the AI audit log when the user allocates.
  - `Compliance.js`: new "Suggested fix" column on the document risk table (loads field suggestions for high/medium risk rows in the top 25). Clicking "Apply" logs the decision and navigates to the invoice detail.
  - `AiActivity.js`: new `/ai` sidebar entry (Auditor read-only by backend authorization; Admin/Accountant can re-trigger Rejected). Filters by suggestionType and status; rejected rows expose a regenerate button.
- ~60 new i18n keys added across TR / EN / DE (`aiActivity`, `aiSuggestions`, `aiSuggestionPill*`, `aiSuggestionType*`, `aiSuggestion*` status names, `aiCategory*` labels, `aiCategoryRationale_*`, `aiFieldRationale_*`, `aiSuggestedFix`, `aiSuggestionApply`, `aiMatchConfidence`, `aiAutoPreselected`, `aiNoSuggestions`, `aiRecentSuggestions`).
- `IAiSuggestionRepository` → `AiSuggestionRepository` and the four suggesters wired in `EasyMitt.Infrastructure.DependencyInjection`. `AiEndpoints` registered in `Program.cs`.

Latest validation known to pass:

```powershell
dotnet build .\service\EasyMitt.slnx
# Build succeeded. 0 Warning(s). 0 Error(s).
cd ui
npm run lint
# clean
npm run build
# Built in ~2.8s.
```

Live end-to-end smoke test (admin@easymitt.local / Admin123!):
- `POST /api/v1/ai/datev-suggest {documentType:"Expense",documentId:<id>}` → envelope `success=true`, data `{account:"4806", taxKey:"9", vatRate:19, confidence:0.95, rationale:"category_mapped", matchedRule:"settings.expense_mapping"}`.
- `POST /api/v1/ai/category-suggest {vendorName:"Lufthansa", lineDescriptions:"Flight FRA-BER", ...}` → `{category:"Travel", confidence:0.91, rationale:"keyword_match"}`.
- `GET /api/v1/ai/suggestions` → envelope `success=true, message="AI önerileri yüklendi.", count=0` initially; after `POST /suggestions/log?status=Accepted` count=1, status=Accepted.
- `GET /api/v1/ai/payment-suggest/{txId}` → returns invoice match with confidence 0.63 (reasons: amount_exact + date_close), autoPreselect=false.
- API on 5095 + UI on 5173 verified up.

The previous completed module is `Customer Portal`:

- Backend entity: `CustomerPortalAccessEntity` → `customer_portal_access` table with token_hash (SHA-256, unique), token_prefix (first 8 chars for display), status (Active/Revoked), expires_at_utc, last_used_at_utc, created_by_user_email.
- Backend: `ICustomerPortalAccessRepository` / `CustomerPortalAccessRepository` (Infrastructure).
- Backend: `IPortalTokenGenerator` / `PortalTokenGenerator` (Infrastructure) — cryptographic 32-byte tokens, URL-safe base64, SHA-256 hashing.
- Backend: `PortalEndpoints` exposes two surfaces:
  - Admin (JWT, InvoiceWrite for mutation): `GET /api/v1/customers/{id}/portal-access`, `POST /api/v1/customers/{id}/portal-access`, `POST /api/v1/customers/portal-access/{tokenId}/revoke`.
  - Public portal (token bearer via `X-Portal-Token` header or `?token=` query, `AllowAnonymous`): `GET /api/v1/portal/me`, `GET /api/v1/portal/invoices`, `GET /api/v1/portal/invoices/{id}`, `GET /api/v1/portal/invoices/{id}/zugferd.pdf`, `GET /api/v1/portal/invoices/{id}/xrechnung.xml`, `GET /api/v1/portal/quotes`, `GET /api/v1/portal/quotes/{id}`, `POST /api/v1/portal/quotes/{id}/accept`, `POST /api/v1/portal/quotes/{id}/decline`.
- Backend: invoice listing scoped to caller's customer/company; hides Draft and Cancelled invoices and Draft quotes; computes per-invoice amountPaid / amountOpen / isOverdue against PaymentAllocations and BT-9 due date.
- Backend: token issuance returns plaintext token + sharable portal URL only once. Revocation is soft (status flip). `LastUsedAtUtc` is touched on every successful portal call. Expired tokens are filtered server-side.
- Backend: `MessageKeys` for portal added with EN / TR / DE localizations (PortalTokenIssued, PortalTokenRevoked, PortalInvalidToken, PortalSessionFound, PortalInvoicesFound, PortalInvoiceFound, PortalQuotesFound, PortalQuoteFound, PortalQuoteAccepted, PortalQuoteDeclined, PortalQuoteNotResponsive, PortalInvoiceNotFound, PortalQuoteNotFound, PortalTokensFound, PortalTokenNotFound).
- Migration: `20260516184328_CustomerPortalAccess` applied.
- Frontend: `ui/src/api/portal.js` — both portal session API (X-Portal-Token header, sessionStorage) and admin portalAccessApi (JWT bearer) in one module.
- Frontend: `ui/src/components/PortalLayout.js` — separate dark-sidebar / white-topbar shell with only portal nav (overview / invoices / quotes), portal language picker, "exit" button that clears token.
- Frontend: `ui/src/pages/PortalShell.js` — handles language, session lookup, and `/portal/*` sub-routing outside the staff `Layout`.
- Frontend: `ui/src/pages/PortalEntry.js` (paste-token landing, auto-submits if `?token=` is present).
- Frontend pages: PortalDashboard (KPI cards + recent invoices/quotes), PortalInvoices (table with PDF download), PortalInvoiceDetail (lines + matched payments + ZUGFeRD PDF and XRechnung XML downloads), PortalQuotes (table), PortalQuoteDetail (lines + accept/decline buttons).
- Frontend admin: `CustomerForm.js` gains a `PortalAccessPanel` (issue/revoke tokens, copy plaintext token + portal URL once, list active tokens with prefix + last-used timestamp).
- Routes: `/portal/*` mounted in `main.js` before the staff auth-gated routes so portal access does not require a staff session.
- i18n: ~80 new keys added to `ui/src/i18n.js` for TR / EN / DE (portal layout, dashboard, invoices, quotes, access management).

Latest validation known to pass:

```powershell
dotnet build .\service\EasyMitt.slnx
# Build succeeded. 0 Warning(s). 0 Error(s).
cd ui
npm run lint
# clean
npm run build
# Built in ~3.2s.
```

Live end-to-end smoke test (admin@easymitt.local / Admin123!): issued portal token, called `/api/v1/portal/me` (200, returns customer + company), called `/api/v1/portal/invoices` (200, scoped to customer), confirmed bad token → 401, confirmed revoked token → 401.

The previous completed module is `Reporting Dashboard`:

- Backend: `IReportingRepository` / `ReportingRepository` (Infrastructure) aggregating from `InvoiceDrafts`, `PaymentAllocations`, `Expenses`, `DatevExportLogs`.
- Backend: `ReportingDtos` (overview, monthly revenue series, VAT summary, DATEV coverage, aging buckets, top customers, expense summary).
- Backend: `ReportingEndpoints` exposes `GET /api/v1/reporting/overview?from=&to=` (InvoiceRead policy: Admin / Accountant / Auditor).
- Backend: `MessageKeys.ReportingOverviewFound` localized in EN / TR / DE.
- Backend UTC-safety: `ReportingRepository.cs` wraps `DateOnly → DateTime` with `DateTime.SpecifyKind(..., DateTimeKind.Utc)` because Npgsql refuses `Unspecified` kind for `timestamp with time zone`.
- Frontend: `ui/src/api/reporting.js` plus `ui/src/pages/Reporting.js` page with KPI cards, monthly revenue chart, VAT split, DATEV coverage gauge, aging buckets table, top customers list, expense-by-category list.
- Frontend UTC-safety: date filters use a local `toLocalIso()` helper instead of `toISOString().slice(0,10)` (which previously shifted Jan 1 → Dec 31 in UTC+3).
- Navigation entry and `/reporting` route added in `Layout.js` / `main.js`.
- ~tr/en/de i18n keys added for KPI labels, aging buckets, chart titles, filters.
- No new DB migration — reads from existing tables.

Latest validation known to pass:

```powershell
dotnet build .\service\EasyMitt.slnx
# Build succeeded. 0 Warning(s). 0 Error(s).
cd ui
npm run lint
# No errors.
npm run build
# Built successfully.
```

The previous completed module is `Email Delivery`:

- Backend: `IEmailService` interface with `EmailMessage` and `EmailSendResult` records in Application layer.
- Backend: `SmtpEmailService` (sends via `System.Net.Mail.SmtpClient`) and `NoOpEmailService` (logs warning, returns success) in Infrastructure.
- Backend: `EmailOptions` configuration class bound from `appsettings.json` `Email` section. NoOp selected when `SmtpHost` is empty.
- Backend: `EmailDeliveryLogEntity` → table `email_delivery_logs` tracking: id, company_id, document_type, document_id, to_email, subject, attachment_type, status (Sent/Failed), error_message, sender_user_id, sender_user_email, created_at_utc.
- Backend: `EmailDeliveryLogEntityConfiguration`, `IEmailDeliveryLogRepository`, `EmailDeliveryLogRepository`.
- Backend: `EmailEndpoints` — POST /api/v1/email/invoices/{id}/send (loads draft, generates ZUGFeRD PDF, sends email, logs), POST /api/v1/email/quotes/{id}/send, POST /api/v1/email/dunning/{id}/send, GET /api/v1/email/invoices/{id}/logs, GET /api/v1/email/quotes/{id}/logs, GET /api/v1/email/logs.
- Backend: Invoice email automatically attaches ZUGFeRD PDF generated via `IElectronicInvoiceGenerator`.
- Backend: `MessageKeys` EmailSent, EmailFailed, EmailLogsFound, EmailDocumentNotFound added with EN/TR/DE localizations.
- Migration: `20260516_EmailDeliveryLogs` applied.
- Frontend: `ui/src/api/email.js` — sendInvoice, getInvoiceLogs, sendQuote, getQuoteLogs, sendDunning, getRecentLogs.
- Frontend: `ui/src/components/SendEmailModal.js` — reusable modal with toEmail/subject/body fields, shows ZUGFeRD attachment hint for invoices.
- Frontend: `InvoiceDetail.js` — "Send Email" button in button-items, email delivery log panel, `SendEmailModal` integration.
- Frontend: `QuoteDetail.js` — "Send Email" button in lifecycle panel, email delivery log panel, `SendEmailModal` integration.
- Frontend: `Dunning.js` — "Send Email" button below "Record Reminder", pre-filled subject/body with dunning context, `SendEmailModal` integration.
- Frontend: ~57 new i18n keys added in TR/EN/DE for all email-related UI text.
- No SMTP is configured by default; `NoOpEmailService` logs and returns success so the UI works without SMTP setup.

Latest validation known to pass:

```powershell
dotnet build .\service\EasyMitt.slnx
# Build succeeded. 0 Warning(s). 0 Error(s).
cd ui
npm run lint
# No errors.
npm run build
# Built in ~2.8s.
```

The previous completed module is `Compliance Center`:

- Backend added `/api/v1/compliance/dashboard` (GET with filters: from, to, status, riskLevel).
- Backend added `/api/v1/compliance/documents/{invoiceDraftId}/timeline` (GET).
- New `IComplianceRepository` interface and `ComplianceRepository` implementation in Infrastructure.
- Compliance repository queries InvoiceDrafts, PaymentAllocations, DunningReminders, and DatevExportLogs - no new migration needed.
- Risk scoring per invoice: high/medium/low/none based on missing BT fields, GoBD archive, DATEV coverage, overdue status.
- XRechnung/ZUGFeRD readiness checks: BT-1, BT-2, BT-5, BT-20, BT-22, BT-34, BT-26 field presence.
- GoBD archived = non-null ArchiveObjectKey.
- DATEV exported = invoice issue date falls within any DATEV export log period.
- Audit timeline aggregates: CreatedAtUtc, IssuedAtUtc, SentAtUtc, ArchiveObjectKey presence, DunningReminders, PaidAtUtc, CancelledAtUtc.
- MessageKeys added: `ComplianceDashboardFound`, `ComplianceTimelineFound`, `ComplianceDocumentNotFound`.
- Localization added for EN, TR, DE in DictionaryAppLocalizer.
- `ComplianceEndpoints` registered in Program.cs.
- `IComplianceRepository` → `ComplianceRepository` registered in Infrastructure DI.
- UI: `ui/src/api/compliance.js` — dashboard and timeline API calls.
- UI: `ui/src/pages/Compliance.js` — central dashboard with readiness cards, document risk table, audit timeline panel, and date/status/risk filters.
- Navigation: `ShieldCheck` icon added to sidebar under Dunning.
- Route: `/compliance` added in main.js.
- i18n keys added to all three languages (TR/EN/DE): compliance, readiness cards, risk levels, risk codes, audit event types, and filter labels.
- No DB migration was needed.

Latest validation known to pass:

```powershell
dotnet build .\service\EasyMitt.slnx
# Build succeeded. 0 Warning(s). 0 Error(s).
cd ui
npm run lint
# No errors.
npm run build
# Built in ~2.5s.
```

## Current Repository State

There are many uncommitted files from previous modules. Do not revert them. They include the SaaS core, quotes, expenses, DATEV, bank payments, Mahnwesen, scan service, UI restructuring, and branding work.

Before making any new changes, run:

```powershell
git status --short
```

Expected high-level dirty state includes:

- `scan-service/`
- SaaS auth/company/customer/product/inventory files
- quote files
- expense files
- DATEV files
- payment files
- dunning files
- UI pages and API clients
- branding assets
- migrations through `20260515205734_DunningReminders`

## Completed Modules

### Backend Foundation

- .NET 10 Clean Architecture / modular monolith structure.
- Common JSON response envelope.
- Global exception handling.
- Localization for `tr`, `en`, `de`.
- Scalar/OpenAPI in development.
- Role-based auth with Admin, Accountant, Auditor.
- PostgreSQL EF Core persistence.

### Germany E-Invoice Core

- EN16931 DTO structure with BT-code JSON mapping.
- XRechnung XML export.
- ZUGFeRD PDF export.
- Peppol submit stub.
- German VAT policy in Domain: `0`, `7`, `19`.
- IBAN policy in Domain with normalization and ISO 13616 mod-97 validation.

### Local Scan Import

- `scan-service` added as a local service.
- EasyMitt API proxies `/api/v1/invoices/ingest/scan` to scan service.
- Ollama vision model path is the default local AI flow.
- Default model: `llama3.2-vision:11b`.
- Default Ollama endpoint: `http://127.0.0.1:11434`.

### SaaS Core

- PostgreSQL-backed companies and users.
- Password hashing.
- Seeded demo users moved into DB seed flow.
- Customer module with B2B/B2C distinction.
- Product/service catalog.
- Inventory movements.
- Tenant-scoped entities and queries.

### Invoice Operations

- Invoice lifecycle: Draft, Issued, Sent, PartiallyPaid, Paid, Overdue, Cancelled.
- Invoice list page.
- Invoice create page with customer and product selection.
- Invoice detail page with lifecycle actions.
- Raw import flow retained.

### Quotes

- Quote list/create/detail/edit.
- Quote lifecycle.
- Convert quote to invoice.

### Expenses

- Expense list/create/edit.
- Receipt/expense scan flow.
- Expense DATEV integration fields.

### DATEV

- Central DATEV page.
- Settings.
- Tax key mappings.
- Preview pages for invoices and expenses.
- Export history and archive metadata.
- Basic CSV and DATEV EXTF direction.
- Force re-export confirmation.

### Bank Payments

- Bank transaction list and manual entry.
- CSV import foundation.
- Matching suggestions.
- Payment allocation to invoices.
- Invoice payment summary.
- Partial payment support.

### Dunning / Mahnwesen

- Overdue receivables overview.
- Customer receivables grouping.
- Reminder history.
- Reminder creation with levels:
  - Friendly reminder
  - 1. Mahnung
  - 2. Mahnung
  - Final notice

### Compliance Center

- Central compliance dashboard at `/compliance`.
- XRechnung, ZUGFeRD, GoBD, DATEV readiness progress cards.
- Document risk list with risk levels (high/medium/low/none) and per-document risk codes.
- Audit timeline per invoice: status transitions, archive events, and dunning reminders.
- Filters: date range (from/to), invoice status, risk level.
- All roles can view (Auditor read-only by backend authorization).
- No new DB migration. Reads from existing tables.

### Email Delivery

- Send invoice/quote/dunning emails via SMTP or NoOp fallback.
- `email_delivery_logs` table tracking status, recipient, subject, attachment, sender.
- Reusable `SendEmailModal` integrated into InvoiceDetail, QuoteDetail, Dunning.
- Migration `20260515233643_EmailDeliveryLogs` applied.

### Reporting Dashboard

- `/api/v1/reporting/overview` endpoint with date range filters.
- Revenue by month, VAT summary (0% / 7% / 19%), DATEV export coverage.
- Overdue receivables aging buckets and top customers (revenue + overdue).
- Expense summary by category.
- `/reporting` UI page with KPIs, charts, tables.
- UTC-safe date handling on backend and frontend.
- No new DB migration. Reads from existing tables.

### Customer Portal

- Per-customer portal access tokens (`customer_portal_access` table), issue/revoke from admin Customer edit screen.
- Public portal endpoints under `/api/v1/portal/*` authenticated via `X-Portal-Token` header (or `?token=` query for magic-link entry).
- Portal pages (separate `PortalLayout`, no staff sidebar): overview KPIs, invoices list/detail, quotes list/detail, ZUGFeRD PDF + XRechnung XML download, accept/decline quote.
- Tokens are stored as SHA-256 hashes; plaintext is shown once at issue.
- TR/EN/DE i18n on every portal surface.

### Advanced AI Accounting

- `ai_suggestions` audit table (Pending / Accepted / Rejected / Superseded), JSON payload per suggestion, decided-by-user email + timestamp.
- Domain heuristics (pure functions, no DB): `ExpenseCategoryHeuristics`, `PaymentMatchScorer` (weighted blend amount 0.6 / IBAN 0.2 / name 0.1 / date 0.1), `DatevAccountHeuristics`, `InvoiceFieldHeuristics` (Leitweg-ID detection, etc.).
- Endpoints under `/api/v1/ai`: `datev-suggest`, `category-suggest`, `payment-suggest/{txId}`, `invoice-field-suggest/{invoiceId}`, `suggestions` (list / accept / reject / retry / log).
- Receipt scan flow attaches a category suggestion + confidence; ExpenseForm renders a yellow accept/reject pill above the Category field.
- DATEV revenue/expense account suggestion driven by `DatevSettings` tax-key + expense-account mappings (source of truth), shown on ExpenseForm + InvoiceDetail.
- Compliance Center risk list has a "Suggested fix" column for high/medium risk rows (e.g. "Buyer VAT looks like a Leitweg-ID → BT-10").
- Payments page shows confidence progress bar per match, auto pre-selects the top suggestion at confidence ≥ 0.85.
- New `/ai` sidebar page (AiActivity): filter by type+status, retry rejected suggestions (creates new Pending + supersedes old).
- Reusable `<AiSuggestionPill />` component with TR/EN/DE labels.
- Migration `20260519101616_AiSuggestions` applied.

### Production Hardening

- Secrets out of `appsettings.json`; `appsettings.Development.json` for local dev or `EASYMITT__Section__Key` env override; startup warning + `/health/ready` flag when `Authentication:SigningKey` missing.
- Pluggable archive (`Archive:Backend = Local | S3`): `LocalFileImmutableArchiveStore` + `S3ObjectLockArchiveStore` (Object Lock COMPLIANCE). `POST /api/v1/compliance/verify-archive/{invoiceId}` (Admin-only) re-reads and SHA-256 matches.
- Schematron rule engine (Domain pure): BR-02, BR-03, BR-05, BR-06, BR-07, BR-16, BR-CO-15, BR-DE-01, BR-DE-15, BR-DE-16, BR-DE-21, BR-DE-23, BR-DE-26. `POST /api/v1/invoices/{id}/validate-schematron`. Compliance dashboard exposes `isSchematronValid` + `schematronFailureCodes`; risks include `schematron_*` codes.
- Quartz.NET background jobs: `EmailRetryJob` (15-min cron), `OverdueInvoiceJob` (daily), `DatevExportScheduledJob` (opt-in). `GET /api/v1/admin/jobs` + `POST /api/v1/admin/jobs/{name}/run` (Admin-only). `JobRunHistory` tracks last-run + last-status + last-error.
- Serilog request logging (JSON in production, console in dev) with `TraceId` / `CompanyId` / `UserId` enrichment. OpenTelemetry (ASP.NET + HttpClient + EF Core) with OTLP exporter activated via `Telemetry:OtlpEndpoint`. `/health/live` + `/health/ready` (db + archive + secrets).
- Production Peppol AP adapter `PartnerGatewayInvoiceDispatch` behind `Dispatch:Backend`. New `dispatch_logs` table + `IDispatchLogRepository`. Compliance dashboard gains "Dispatched" readiness card + table column + audit timeline event.
- Postmark transactional email adapter (`Email:Backend = NoOp | Smtp | Postmark`).
- New `AuthorizationPolicies.AdminOnly`. Admin endpoints + verify-archive enforce Admin role server-side.
- `/admin/jobs` UI page (Admin-only sidebar entry); Compliance Center adds Schematron + Dispatched readiness cards + columns; InvoiceDetail adds "Validate Schematron" + "Verify archive" buttons.
- Migration `20260519144130_DispatchLogs` applied.

## Local Services

Default ports:

- API: `http://127.0.0.1:5095`
- UI: `http://127.0.0.1:5173`
- PostgreSQL: `localhost:5432`
- Scan service: `http://127.0.0.1:7332`
- Ollama: `http://127.0.0.1:11434`

PostgreSQL connection:

```text
Host=localhost;Port=5432;Database=easymitt;Username=postgres;Password=postgres
```

Demo users:

- `admin@easymitt.local` / `Admin123!` / `Admin` / `tr`
- `accountant@easymitt.local` / `Accountant123!` / `Accountant` / `de`
- `auditor@easymitt.local` / `Auditor` / `Auditor123!` / `en`

## Important Constraints

- UI must stay JavaScript-only.
- Do not add TypeScript files.
- Do not add a new UI framework.
- All UI text must be in `ui/src/i18n.js` for TR/EN/DE.
- Do not trust UI-only authorization.
- Keep Germany-only compliance focus.
- Do not add other-country support to this project.
- Keep file export success responses as direct file downloads.

## Suggested Next Work

Production Hardening is now complete. The next recommended large module is `LLM-Backed AI Suggestions`, because:

- The deterministic heuristic suggesters in `EasyMitt.Domain.Accounting` already cover the easy wins; the remaining lift on category / field correction quality is better handled by a vision-or-text LLM.
- The local Ollama path (`scan-service`) is already wired in for OCR — extending it to suggestion endpoints reuses an existing local-first dependency rather than adding a new cloud bill.
- Interfaces (`IExpenseCategorySuggester`, `IMissingFieldSuggester`, `IDatevAccountSuggester`) are already in place behind DI, so swapping implementations is contained.

Recommended scope for `LLM-Backed AI Suggestions`:

- New Infrastructure adapters: `OllamaExpenseCategorySuggester`, `OllamaMissingFieldSuggester` (and optionally `OllamaInvoiceLineExtractor` for receipt scans) calling local Ollama (`/api/generate` JSON mode).
- `Ai:Backend = Heuristic | Ollama` config + DI selector; heuristic remains the fallback when Ollama is unreachable.
- Track `payload.source = "heuristic" | "ollama"` on each `ai_suggestions` row so the UI/audit can show provenance.
- Latency budgets (timeout → fall back to heuristic) and confidence calibration.

Alternative next module: `Multi-Tenant Operations` — admin company settings page, user management page, invite/deactivate flows, and a per-tenant audit log surface.

## How To Continue In Another Agent

Paste one of the prompts below into Claude Code, Codex, or another agent at the start of a new session.

### A) Continue the next planned module

Use this when you want the agent to pick up the module listed in "Suggested Next Work":

```text
This is the EasyMitt monorepo at C:\Github Projects\EasyMitt.

Before writing any code:
1. Read AGENTS.md (project rules — Service Lifecycle Rule and Feature Completion Rule are mandatory).
2. Read docs/agent-handoff.md (current state, latest completed module, next recommended work).
3. Read docs/roadmap.md (completed vs. upcoming modules).
4. Read docs/decisions.md and docs/runbook.md if relevant to the upcoming work.
5. Run `git status --short` to see uncommitted changes — DO NOT revert them.

Then:
- Implement the module listed under "Suggested Next Work" end-to-end in one coherent pass.
- Backend (Clean Architecture), DB migration if needed, endpoints with the common response envelope, role-based auth, frontend pages/components, i18n keys for TR/EN/DE.
- Validate with: `dotnet build .\service\EasyMitt.slnx`, `npm run lint`, `npm run build`.
- After finishing, follow AGENTS.md Service Lifecycle Rule (ensure API on 5095 and UI on 5173 are running) and Feature Completion Rule (mark done in both roadmap.md and agent-handoff.md).
```

### B) Work on a specific feature you name

Use this when you want to override the "next" recommendation:

```text
This is the EasyMitt monorepo at C:\Github Projects\EasyMitt.

Before writing any code:
1. Read AGENTS.md, docs/agent-handoff.md, docs/roadmap.md.
2. Run `git status --short` — DO NOT revert existing changes.

Then implement the following feature end-to-end: <DESCRIBE FEATURE HERE>

Follow all project rules: Clean Architecture, JS-only UI, common response envelope, Germany-only, role-based auth, TR/EN/DE i18n.
Validate (dotnet build, npm lint, npm build). When done, follow the Service Lifecycle Rule and Feature Completion Rule from AGENTS.md.
```

### C) Quick continuation (when context is fresh / same day)

```text
EasyMitt monorepo. Read docs/agent-handoff.md and continue from "Suggested Next Work". Follow AGENTS.md rules. Don't revert existing changes.
```
