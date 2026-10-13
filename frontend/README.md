# Frontend

Angular 22 (zoneless) UI for stock management.

From this folder:

```bash
npm install
npm start
```

Opens http://localhost:4200. `/api` is proxied to the .NET API at http://localhost:5011 — run the API first (`dotnet run` in `../api`).

See the [repository README](../README.md) for API setup.

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

