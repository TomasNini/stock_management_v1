# Stock management

Angular 22 frontend and .NET 10 API. The UI talks to the API through a relative `/api` path (proxied in development).

## Requirements

| | Version |
| --- | --- |
| Node.js | `^22.22.3` or `^24.15.0` or `>=26` (Angular CLI 22) |
| Yarn | `>= 1.13` |
| .NET SDK | `10.0` |

Check versions:

```bash
node -v
yarn -v
dotnet --version
```

## Run

From the repository root.

```bash
yarn api
yarn frontend
```

API: http://localhost:5011  
Frontend: http://localhost:4200  

Pending EF Core migrations are applied on API startup. `ng serve` proxies `/api` to `http://localhost:5011`.

First time for the UI:

```bash
cd frontend
yarn install
```

## Tests

### API (xUnit)

HTTP tests live in `api.Tests`. They start the API in-process against an in-memory SQLite database and never touch `stock_management.db`. The frontend has no test runner configured.

From the repository root:

```bash
dotnet test
```

Run a single test by name:

```bash
dotnet test --filter GetProducts_WhenEmpty_ReturnsEmptyList
```

## Migrations

Install the tool once:

```bash
dotnet tool install --global dotnet-ef
```

After changing entities or `MainDbContext`:

```bash
dotnet ef migrations add <Name> --output-dir Infrastructure/Data/Migrations
```

Apply to the database (also happens automatically on `dotnet run`):

```bash
dotnet ef database update
```

List migrations:

```bash
dotnet ef migrations list
```

Remove the last unapplied migration:

```bash
dotnet ef migrations remove
```
