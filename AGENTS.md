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

## Service Lifecycle Rule

After every development task that touches backend or frontend code, you MUST verify both services are running and restart any that are down. The user expects to keep working immediately after you finish — a stopped service is a broken handoff.

Required checks at the end of every task that built, migrated, or modified the backend or UI:

1. **Backend API on port 5095** (the port the UI's Vite proxy targets):
   ```powershell
   try { Invoke-RestMethod -Uri "http://127.0.0.1:5095/health" -TimeoutSec 3 } catch { "down" }
   ```
   If down, start it detached so it survives this shell exiting:
   ```powershell
   cmd /c 'start "EasyMitt API" /D "C:\Github Projects\EasyMitt\service\src\EasyMitt.Api" /MIN cmd /c "set ASPNETCORE_URLS=http://127.0.0.1:5095 && dotnet run --no-launch-profile"'
   ```
   Wait ~10 seconds, then re-check the health endpoint.

2. **UI dev server on port 5173**:
   ```powershell
   try { (Invoke-WebRequest -Uri "http://127.0.0.1:5173" -TimeoutSec 2 -UseBasicParsing).StatusCode } catch { "down" }
   ```
   If down, start it detached:
   ```powershell
   cmd /c 'start "EasyMitt UI" /D "C:\Github Projects\EasyMitt\ui" /MIN cmd /c "npm run dev"'
   ```

3. **Scan service on port 7332** — only restart if your task touched `scan-service/` or invoice ingest flow.

Important notes:
- Do NOT use PowerShell `Start-Job` to host the API/UI — the process dies when the job's parent shell exits. Always use `cmd /c start` for true detachment.
- Do NOT use `dotnet run` without `--no-launch-profile` — `launchSettings.json` overrides to port 5180 and breaks the UI proxy.
- If port 5095 is already bound by an old instance, do not assume it's healthy — verify with `/health`. If it's stuck, find the PID via `Get-NetTCPConnection -LocalPort 5095` and `Stop-Process`.
- DLL build locks: if `dotnet build` fails with `MSB3027` (file locked by `EasyMitt.Api`), stop that PID first, rebuild, then restart per step 1.

## Feature Completion Rule

Every completed feature MUST be marked as done in BOTH of these files before the task is reported finished. Otherwise the next session (Claude, Codex, or the user reopening the project) cannot tell what's done and may redo or skip work.

**`docs/roadmap.md`:**
- Move the feature's section out of `## Upcoming Modules` or `## Next Major Module` into `## Completed`.
- Replace the planned bullets with a short list of what actually shipped (key endpoints, tables, components).
- Bump `Last updated: YYYY-MM-DD` at the top to today's date.
- Add the next chosen module under `## Next Major Module` with a Goal line and scope bullets.

**`docs/agent-handoff.md`:**
- Bump `Last updated: YYYY-MM-DD (Module Name)` at the top.
- Replace `## Latest Completed Work` with the new module's details: backend files, migrations, frontend files, i18n keys count, validation commands that passed.
- Move the previous "Latest" into the prose ("The previous completed module is …") so history is preserved.
- Rewrite `## Suggested Next Work` to recommend the next module with a one-line rationale and a scope bullet list.
- If a new DB migration was applied, list its name. If services have a new port or new env var, update `## Local Services`.

Do not skip this step even for "small" modules. The user opens new sessions frequently — accurate handoff docs are how we avoid losing context.

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
- Honor the **Service Lifecycle Rule** and **Feature Completion Rule** above before reporting a task as done.
