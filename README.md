# EasyMitt

Monorepo for the EasyMitt MVP.

## Structure

- `service/` - .NET 10 API, application, domain, infrastructure projects.
- `ui/` - React/Vite frontend.
- `scan-service/` - Local Ollama-powered invoice/receipt image analyzer.

## Local Services

PostgreSQL connection used by the API:

```text
Host=localhost;Port=5432;Database=easymitt;Username=postgres;Password=postgres
```

Run PostgreSQL locally:

```powershell
docker run --name easymitt-postgres -e POSTGRES_DB=easymitt -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v easymitt-postgres-data:/var/lib/postgresql/data -d postgres:16
```

## API

Backend solution:

```text
service/EasyMitt.slnx
```

```powershell
dotnet run --project service/src/EasyMitt.Api/EasyMitt.Api.csproj
```

Scalar docs are available in Development at:

```text
http://localhost:<port>/docs
```

## UI

```powershell
cd ui
npm install
npm run dev
```

## Scan Service

The scan service receives invoice/receipt images from the API and analyzes them with a local Ollama vision model.

```powershell
cd scan-service
npm install
npm run dev
```

Default endpoints:

```text
Scan service: http://127.0.0.1:7332
Ollama:       http://127.0.0.1:11434
Model:        llama3.2-vision:11b
```

Install the local vision model:

```powershell
ollama pull llama3.2-vision:11b
```
