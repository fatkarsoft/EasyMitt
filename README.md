# EasyMitt

Monorepo for the EasyMitt MVP.

## Structure

- `service/` - .NET 10 API, application, domain, infrastructure projects.
- `ui/` - React/Vite frontend.

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
