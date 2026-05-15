# EasyMitt Runbook

Last updated: 2026-05-16

## Prerequisites

- .NET 10 SDK
- Node.js / npm
- PostgreSQL 16
- Docker, optional for local PostgreSQL
- Ollama, optional for local scan import

## Repository

```powershell
cd "C:\Github Projects\EasyMitt"
```

## PostgreSQL

Connection string used by local appsettings:

```text
Host=localhost;Port=5432;Database=easymitt;Username=postgres;Password=postgres
```

Start local PostgreSQL with Docker:

```powershell
docker run --name easymitt-postgres -e POSTGRES_DB=easymitt -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v easymitt-postgres-data:/var/lib/postgresql/data -d postgres:16
```

If the container already exists:

```powershell
docker start easymitt-postgres
```

## Backend

Build:

```powershell
dotnet build .\service\EasyMitt.slnx
```

Run API:

```powershell
dotnet run --project service/src/EasyMitt.Api/EasyMitt.Api.csproj --urls http://127.0.0.1:5095
```

Docs:

```text
http://127.0.0.1:5095/docs
http://127.0.0.1:5095/openapi/v1.json
```

If build fails because DLLs are locked, find and stop the API process:

```powershell
Get-NetTCPConnection -LocalPort 5095 -ErrorAction SilentlyContinue | Select-Object LocalAddress,LocalPort,State,OwningProcess
Stop-Process -Id <PID> -Force
```

## EF Core Migrations

Add migration:

```powershell
dotnet ef migrations add MigrationName --project service/src/EasyMitt.Infrastructure/EasyMitt.Infrastructure.csproj --startup-project service/src/EasyMitt.Api/EasyMitt.Api.csproj --output-dir Persistence/Migrations
```

Apply migrations:

```powershell
dotnet ef database update --project service/src/EasyMitt.Infrastructure/EasyMitt.Infrastructure.csproj --startup-project service/src/EasyMitt.Api/EasyMitt.Api.csproj
```

## Frontend

Install dependencies:

```powershell
cd ui
npm install
```

Run dev server:

```powershell
npm run dev
```

Default UI:

```text
http://127.0.0.1:5173
```

Validate:

```powershell
npm run lint
npm run build
```

## Scan Service

Install dependencies:

```powershell
cd scan-service
npm install
```

Run scan service:

```powershell
npm run dev
```

Default scan service:

```text
http://127.0.0.1:7332
```

Install Ollama model:

```powershell
ollama pull llama3.2-vision:11b
```

Ollama endpoint:

```text
http://127.0.0.1:11434
```

If `ollama serve` says port is already in use, Ollama is already running as a background service.

## Demo Users

```text
admin@easymitt.local / Admin123! / Admin / tr
accountant@easymitt.local / Accountant123! / Accountant / de
auditor@easymitt.local / Auditor123! / Auditor / en
```

## Smoke Test Snippets

Login:

```powershell
$login = Invoke-RestMethod -Uri 'http://127.0.0.1:5095/api/v1/auth/login' -Method Post -ContentType 'application/json' -Body (@{ email='admin@easymitt.local'; password='Admin123!' } | ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.data.accessToken)" }
```

Check auth:

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:5095/api/v1/auth/me' -Headers $headers
```

Check dunning:

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:5095/api/v1/dunning/overview' -Headers $headers
```

Check DATEV settings:

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:5095/api/v1/datev/settings' -Headers $headers
```

## End Of Session Checklist

Before handing the repo to another agent:

```powershell
git status --short
dotnet build .\service\EasyMitt.slnx
cd ui
npm run lint
npm run build
```

Then update:

- `docs/agent-handoff.md`
- `docs/roadmap.md` if product scope changed
- `docs/decisions.md` if a lasting decision changed
