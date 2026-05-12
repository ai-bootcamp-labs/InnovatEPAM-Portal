# InnovatEPAM Portal — Backend

.NET 8 Web API monorepo with four production projects and two test projects.

## Layout

```text
backend/
  InnovatEpam.Portal.sln
  Directory.Build.props          # Shared .NET 8 + nullable + warnings-as-errors
  .config/dotnet-tools.json      # Local tools: dotnet-ef, dotnet-format
  src/
    InnovatEpam.Portal.Api/             # ASP.NET Core Web API host
    InnovatEpam.Portal.Application/     # Use cases, DTOs, validators, ports
    InnovatEpam.Portal.Domain/          # Aggregates, entities, enums
    InnovatEpam.Portal.Infrastructure/  # EF Core, identity, storage, persistence
  tests/
    InnovatEpam.Portal.UnitTests/         # xUnit + FluentAssertions
    InnovatEpam.Portal.IntegrationTests/  # WebApplicationFactory + Testcontainers.PostgreSql
```

## First-time setup

```powershell
# From backend/
dotnet tool restore
dotnet restore
dotnet build
dotnet test
```

## EF Core migrations

```powershell
dotnet ef migrations add <Name> `
  --project src/InnovatEpam.Portal.Infrastructure `
  --startup-project src/InnovatEpam.Portal.Api

dotnet ef database update `
  --project src/InnovatEpam.Portal.Infrastructure `
  --startup-project src/InnovatEpam.Portal.Api
```

See [../specs/001-phase1-mvp/quickstart.md](../specs/001-phase1-mvp/quickstart.md)
for the full local-dev setup including Docker PostgreSQL.
