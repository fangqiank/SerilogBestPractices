# Repository Guidelines

## Project Structure & Module Organization

```
SerilogBestPractices/
├── Behaviors/            # MediatR pipeline behaviors (e.g., LoggingBehavior)
├── Data/                 # SQLite persistence via Dapper (Repository pattern)
├── Events/               # Domain events (INotification) + handlers (pub-sub)
├── Features/Orders/      # One file per use case: Command/Query record + Handler in same file
├── Models/               # Domain models (Order, OrderStatus)
├── Program.cs            # Console entry point: Serilog config, DI, demo scenarios
└── appsettings*.json     # Environment-specific Serilog configuration
```

Namespace pattern: `SerilogBestPractices.{Module}` (e.g., `SerilogBestPractices.Features.Orders`).

## Build, Test, and Development Commands

```powershell
dotnet build SerilogBestPractices/SerilogBestPractices.csproj   # Compile the project
dotnet run --project SerilogBestPractices/SerilogBestPractices.csproj  # Run the demo (7 scenarios)
docker-compose up -d seq     # Start Seq log viewer at http://localhost:8081 (compose file in repo root)
```

Set `DOTNET_ENVIRONMENT=Production` to switch to Warning-level logging.

## Coding Style & Naming Conventions

- **Primary constructors**: Use C# primary constructor syntax for DI injection throughout.
- **Structured logging**: Always use Serilog message templates with named placeholders — never string interpolation (e.g., ✅ `LogInformation("Processing {OrderId}", id)` — ❌ `LogInformation($"Processing {id}")`). Interpolation defeats structured log parsing and search.
- **Records for CQRS**: Define commands/queries as `record` types implementing `IRequest<T>`.
- **File-scoped namespaces**: Use `namespace SerilogBestPractices.{Folder};` style.
- **Implicit usings + nullable**: Enabled at project level (`Nullable=enable`, `ImplicitUsings=enable`).

## Testing Guidelines

This project currently has no tests. When adding tests:
- Place them in a `SerilogBestPractices.Tests` test project using xUnit.
- Name test classes `{Target}Tests` and methods `{Method}_{Scenario}_Returns{Expected}`.
- Target 80%+ coverage on handler logic — mock `IOrderRepository` and `IMediator` for unit tests.

## Commit & Pull Request Guidelines

Use conventional commits from the existing history:

```
type(scope): message
```

Types: `feat`, `fix`, `refactor`, `docs`, `chore`. Scope is optional (e.g., `Events`, `Data`).  
Keep the message imperative, lowercase, under 72 characters.

PRs: include a link to the related issue, a concise description of changes, and screenshots for any user-visible output changes.

## Agent-Specific Instructions

- Always target `net10.0` when adding new projects or modifying the `.csproj`.
- MediatR v14 uses `AddOpenBehavior` for generic pipeline behaviors — do not register closed generic types.
- The solution file is `.slnx` (new XML format). Use `dotnet sln SerilogBestPractices.slnx add <path>` when adding projects.
- MediatR requires a license key configured under `MediatR:LicenseKey` in `appsettings.json`.
- When running the app, `serilogdemo.db` (SQLite) is created in the project root — ensure the working directory allows write access.
