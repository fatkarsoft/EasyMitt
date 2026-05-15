# EasyMitt Agent Instructions

This repository is an active EasyMitt monorepo. Read this file first, then read `docs/agent-handoff.md`, `docs/roadmap.md`, `docs/decisions.md`, and `docs/runbook.md` before making changes.

## Product Direction

EasyMitt is a Germany-focused SaaS platform for e-invoicing, accounting operations, and compliance. The product should feel as simple as sevDesk for everyday work, while growing toward Sovos-style compliance strength for German requirements.

Germany is the only target market for this project. Do not add another country flow to this codebase unless the user explicitly changes that decision.

## Repository Shape

- `service/`: .NET 10 backend, Clean Architecture / modular monolith.
- `ui/`: React + Vite frontend. JavaScript only. Do not add `.ts` or `.tsx` files.
- `scan-service/`: local Node service for invoice/receipt image scan using Ollama vision models.

## Backend Rules

- Preserve Clean Architecture boundaries:
  - Domain: policies, constants, value objects, domain rules.
  - Application: DTOs, validation, use-case services, abstractions.
  - Infrastructure: EF Core PostgreSQL, repositories, file/archive integrations, generators, auth implementation.
  - Api: Minimal API endpoints, middleware, response envelope, docs.
- All JSON endpoints must use the common response envelope:
  - `success`
  - `message`
  - `data`
  - `errors`
  - `traceId`
  - `language`
- Keep correct HTTP status codes. Do not turn errors into `200 success:false`.
- Successful file export endpoints return the file directly. Error responses still use the common envelope.
- Authorization is role based:
  - `Admin`: all operations
  - `Accountant`: operational write flows
  - `Auditor`: read-only
- Tenant isolation is mandatory. Company-scoped tables and queries must use `company_id` / token company claim.
- Validation messages must use error codes plus localization. Do not hardcode user-facing validation messages in validators.
- Domain policies stay in Domain. Do not move German VAT, IBAN, retention, or lifecycle rules into UI or endpoint code.

## Frontend Rules

- Use React JavaScript files with `.js` extension only.
- JSX in `.js` is supported by the existing Vite pre-transform plugin.
- Use the existing Veltrix/admin dashboard style:
  - dark vertical sidebar
  - white topbar
  - Bootstrap cards, tables, forms
  - operational, dense, professional SaaS UI
- No landing page. First unauthenticated screen is login.
- New UI text must be added to `ui/src/i18n.js` for `tr`, `en`, and `de`.
- Keep role-based UI behavior, but never rely only on UI for authorization.
- Use existing `ConfirmDialog` instead of browser `confirm`.
- Keep list/create/edit flows professional:
  - list pages show summary cards and table
  - create/edit pages are separate pages
  - include "back to list" navigation
  - responsive layouts are required for all frontend screens

## Local Commands

Run backend build:

```powershell
cd "C:\Github Projects\EasyMitt"
dotnet build .\service\EasyMitt.slnx
```

Run UI checks:

```powershell
cd "C:\Github Projects\EasyMitt\ui"
npm run lint
npm run build
```

Run EF migrations:

```powershell
cd "C:\Github Projects\EasyMitt"
dotnet ef migrations add MigrationName --project service/src/EasyMitt.Infrastructure/EasyMitt.Infrastructure.csproj --startup-project service/src/EasyMitt.Api/EasyMitt.Api.csproj --output-dir Persistence/Migrations
dotnet ef database update --project service/src/EasyMitt.Infrastructure/EasyMitt.Infrastructure.csproj --startup-project service/src/EasyMitt.Api/EasyMitt.Api.csproj
```

## Collaboration Protocol

- Do not revert user changes or unrelated agent changes.
- Before starting work, inspect `git status --short`.
- If the API build fails because DLLs are locked, find and stop the local `EasyMitt.Api` process before rebuilding.
- At the end of every substantial session, update `docs/agent-handoff.md` with:
  - what changed
  - validation commands and results
  - DB migrations applied
  - current services/processes
  - next recommended work
- Keep `docs/roadmap.md` and `docs/decisions.md` current when product direction changes.
