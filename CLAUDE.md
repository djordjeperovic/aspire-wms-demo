# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## AI Assistant Guidance

**Always use Context7 MCP when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.**

## Build & Run Commands

```bash
# Build solution
dotnet build AspireWms.sln

# Run full system (requires Docker Desktop)
dotnet run --project src/AspireWms.AppHost

# Unit tests (no Docker required, TUnit runner)
dotnet run --project tests/AspireWms.UnitTests -c Release

# Functional tests (requires Docker, starts full Aspire AppHost)
dotnet run --project tests/AspireWms.FunctionalTests -c Release
```

Tests use **TUnit** (not xUnit/NUnit). Test assertions use `await Assert.That(...)` syntax. Parameterized tests use `[Arguments(...)]`.

## Architecture

**Modular monolith** built with .NET Aspire (targeting .NET 10.0). Services orchestrated via `AppHost.cs`:
- **YARP API Gateway** (port 5000) — strips `/api` prefix and routes to the API
- **API service** — modular monolith at `src/AspireWms.Api/`
- **PostgreSQL** (`wmsdb`) — single database, one schema per module (e.g., `inventory`, `inbound`, `outbound`)
- **Redis** — caching layer

### Module Structure

Each module under `src/AspireWms.Api/Modules/{ModuleName}/` implements `IModule` (static abstract interface) and follows vertical slice architecture:

```
Modules/{Name}/
├── {Name}Module.cs          # IModule impl: RegisterServices + MapEndpoints
├── Domain/Entities/          # Rich domain entities with factory methods
├── Features/{Feature}/       # Vertical slices: Query/Command + Handler + Validator + Endpoints
└── Infrastructure/           # DbContext, EF configurations, migrations, seeders
```

**Adding a new module:** Create the module directory, implement `IModule`, register it in `Program.cs`, and add a DbContext with its own schema.

### Key Patterns

- **CQRS via MediatR 12.5.0** — Commands and Queries with pipeline behaviors (validation, logging) in `Shared/Infrastructure/Behaviors/`
- **Result\<T\> pattern** — `Shared/Domain/Result.cs` for explicit error handling; use `.Match()` to handle success/failure
- **Value objects** — `Quantity` (non-negative) and `Money` (currency-aware) in `Shared/Domain/ValueObjects/`
- **FluentValidation** — validators auto-discovered and run via `ValidationBehavior` pipeline
- **EF Core schema isolation** — each module's DbContext uses `HasDefaultSchema("{module}")` and its own migrations history table

### EF Core Migrations

Migrations live in each module's `Infrastructure/Migrations/` directory. The inventory module's context is `InventoryDbContext`. Database is auto-migrated on startup in `Program.cs`.

## Currently Implemented

Only the **Inventory module** is complete (Products, Locations, Stock with CRUD + caching). Inbound and Outbound modules are stubs with health endpoints only.
