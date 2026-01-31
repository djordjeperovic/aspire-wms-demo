# Contributing to Aspire WMS Demo

Thank you for your interest in contributing! This is a portfolio/learning project, but contributions are welcome.

## Getting Started

1. Fork the repository
2. Clone your fork
3. Create a feature branch (`git checkout -b feature/amazing-feature`)
4. Make your changes
5. Run tests (`dotnet run --project tests/AspireWms.UnitTests -c Release`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## Development Setup

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- VS Code or Visual Studio 2022+

### Running Locally
```bash
# Start all services
dotnet run --project src/AspireWms.AppHost

# Run tests
dotnet run --project tests/AspireWms.UnitTests -c Release
dotnet run --project tests/AspireWms.FunctionalTests -c Release
```

## Code Style

- Follow existing patterns in the codebase
- Use meaningful commit messages
- Add tests for new features
- Update documentation as needed

## Project Structure

```
src/AspireWms.Api/
├── Shared/           # Cross-cutting concerns
│   ├── Domain/       # Result<T>, Value Objects
│   ├── Infrastructure/  # MediatR behaviors
│   └── Contracts/    # IModule interface
└── Modules/          # Feature modules
    ├── Inventory/    # Products, Locations, Stock
    ├── Inbound/      # Purchase Orders (planned)
    └── Outbound/     # Orders, Shipping (planned)
```

## Adding a New Module

1. Create folder under `src/AspireWms.Api/Modules/{ModuleName}/`
2. Implement `IModule` interface
3. Create DbContext with separate schema
4. Add vertical slice features (Query/Command + Handler + Endpoint)
5. Register in `Program.cs`
6. Add tests

## Questions?

Feel free to open an issue for discussion.
