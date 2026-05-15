# EasyMitt Agent Handoff

Last updated: 2026-05-16

## Current Goal

EasyMitt is being evolved into a Germany-only SaaS platform for e-invoicing, accounting operations, and compliance. The product direction is: sevDesk-level ease of use plus Sovos-like compliance strength for German workflows.

The immediate development style requested by the user is to complete large modules in one coherent pass instead of splitting them into many small partial improvements.

## Latest Completed Work

The latest completed module is `Tahsilat / Mahnwesen`:

- Backend added `/api/v1/dunning`.
- Dunning reminder persistence added with `dunning_reminders`.
- Overview returns overdue invoices, customer receivables, open amount totals, reminder due count, and collected amount.
- Reminder creation increments reminder level and can mark invoice as `Overdue`.
- UI added `/dunning` page with summary cards, overdue invoice table, customer receivables, and reminder side panel.
- Invoice detail now shows reminder history.
- Migration applied: `20260515205734_DunningReminders`.
- Smoke test created a past-due invoice and recorded one reminder successfully.

Latest validation known to pass:

```powershell
dotnet build .\service\EasyMitt.slnx
cd ui
npm run lint
npm run build
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

The next large module should likely be `Compliance Center`, because EasyMitt already has building blocks for:

- XRechnung
- ZUGFeRD
- GoBD archive basics
- DATEV export
- Bank payment matching
- Mahnwesen
- Audit/export history

Recommended scope for Compliance Center:

- Central compliance dashboard.
- GoBD archive status per invoice/expense/export.
- XRechnung/ZUGFeRD readiness status.
- DATEV readiness and last export status.
- Missing-field and validation risk summary.
- Audit timeline view per document.
- Admin/Accountant/Auditor read/write behavior.

## How To Continue In Another Agent

Prompt to paste into Claude Code, Codex, or another agent:

```text
This is the EasyMitt monorepo. First read:
- AGENTS.md
- docs/agent-handoff.md
- docs/roadmap.md
- docs/decisions.md
- docs/runbook.md
- README.md

Then run git status --short and inspect the current code before editing.
Do not revert existing changes.
Continue from the current handoff and preserve all project rules.
```
