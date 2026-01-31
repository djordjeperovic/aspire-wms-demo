# Aspire WMS Demo

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Aspire](https://img.shields.io/badge/Aspire-13.1-512BD4)](https://learn.microsoft.com/en-us/dotnet/aspire/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-127%20passing-brightgreen)](tests/)

A cloud-native **Warehouse Management System** built with .NET Aspire, demonstrating **Modular Monolith** architecture with YARP API Gateway, vertical slice features, and comprehensive testing.

## 🎯 Overview

This portfolio project showcases a minimal WMS for small e-commerce operations:

| Module | Status | Features |
|--------|--------|----------|
| **Inventory** | ✅ Complete | Products, Locations, Stock levels, Movement history |
| **Inbound** | 🔲 Planned | Purchase orders, Receiving, Put-away |
| **Outbound** | 🔲 Planned | Orders, Picking, Packing, Shipping |
| **Frontend** | 🔲 Planned | React dashboard with real-time updates |

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Aspire AppHost                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐         ┌─────────────────────────────────┐   │
│  │   React     │         │        YARP API Gateway         │   │
│  │  Frontend   │────────▶│  localhost:5000                 │   │
│  │             │         │  • /api/inventory/*             │   │
│  └─────────────┘         │  • /api/inbound/*               │   │
│                          │  • /api/outbound/*              │   │
│                          └──────────────┬──────────────────┘   │
│                                         │                       │
│                                         ▼                       │
│              ┌──────────────────────────────────────────┐      │
│              │     Modular Monolith API                 │      │
│              │  ┌────────────┬────────────┬──────────┐  │      │
│              │  │ Inventory  │  Inbound   │ Outbound │  │      │
│              │  │  Module    │  Module    │  Module  │  │      │
│              │  │ (schema)   │ (schema)   │ (schema) │  │      │
│              │  └────────────┴────────────┴──────────┘  │      │
│              └──────────────────────────────────────────┘      │
│                          │                                      │
│              ┌───────────┴───────────┐                         │
│              ▼                       ▼                         │
│       ┌─────────────┐         ┌─────────────┐                  │
│       │ PostgreSQL  │         │    Redis    │                  │
│       │  (wmsdb)    │         │   (cache)   │                  │
│       └─────────────┘         └─────────────┘                  │
└─────────────────────────────────────────────────────────────────┘
```

### Key Architectural Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Architecture** | Modular Monolith | Easy to develop, can split to microservices later |
| **API Gateway** | YARP (containerized) | .NET native, Aspire integration |
| **Database** | Single DB, separate schemas | Cross-module queries, easy microservices extraction |
| **CQRS** | MediatR 12.5.0 | Free MIT license, vertical slice pattern |
| **Validation** | FluentValidation | Pipeline behavior integration |
| **Caching** | Redis | Product catalog caching |
| **Testing** | TUnit + Aspire.Hosting.Testing | Modern test framework with container support |

## ✨ Features Implemented

### Inventory Module
- **Products** - CRUD with SKU validation, Redis caching
- **Locations** - Zone-Aisle-Rack-Bin hierarchy (e.g., "A-01-02-03")
- **Stock** - Quantity tracking with movement audit trail
- **Rich Domain Model** - Encapsulated entities with validation

### Infrastructure
- **Aspire Orchestration** - Service discovery, health checks
- **YARP Gateway** - Path transforms, single entry point
- **OpenTelemetry** - Distributed tracing, metrics, logs
- **Resilience** - Retry policies, circuit breakers

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Aspire CLI](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dotnet-aspire-cli) (optional)

## 🚀 Quick Start

```bash
# Clone the repository
git clone https://github.com/djordjeperovic/aspire-wms-demo.git
cd aspire-wms-demo

# Run with Aspire (starts all containers)
dotnet run --project src/AspireWms.AppHost

# Open Aspire Dashboard (shown in console output)
# Usually: https://localhost:17225
```

### Testing the API

```bash
# Via YARP Gateway (port 5000)
curl http://localhost:5000/api/inventory/products
curl http://localhost:5000/api/inventory/locations
curl http://localhost:5000/api/inventory/health

# Or use the HTTP file in VS Code / Visual Studio
# src/AspireWms.Api/AspireWms.Api.http
```

## 🧪 Running Tests

```bash
# Unit tests (fast, no Docker needed)
dotnet run --project tests/AspireWms.UnitTests -c Release
# 112 tests, ~750ms

# Functional tests (requires Docker)
dotnet run --project tests/AspireWms.FunctionalTests -c Release
# 15 tests, ~48s
```

## 📁 Project Structure

```
aspire-wms-demo/
├── src/
│   ├── AspireWms.AppHost/           # Aspire orchestration
│   ├── AspireWms.ServiceDefaults/   # Shared OpenTelemetry, health checks
│   └── AspireWms.Api/               # Modular Monolith API
│       ├── Shared/                  # Cross-cutting concerns
│       │   ├── Domain/              # Result<T>, Value Objects
│       │   ├── Infrastructure/      # MediatR behaviors
│       │   └── Contracts/           # IModule interface
│       └── Modules/
│           ├── Inventory/           # Products, Locations, Stock
│           ├── Inbound/             # Purchase Orders, Receiving
│           └── Outbound/            # Orders, Picking, Shipping
├── tests/
│   ├── AspireWms.UnitTests/         # Domain & value object tests
│   └── AspireWms.FunctionalTests/   # API endpoint tests
└── docs/
    └── ROADMAP.md                   # Full project roadmap
```

## 📊 API Endpoints

### Inventory Module (`/api/inventory`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/products` | List all products |
| GET | `/products/{id}` | Get product by ID |
| POST | `/products` | Create product |
| PUT | `/products/{id}` | Update product |
| GET | `/locations` | List locations |
| POST | `/locations` | Create location |
| GET | `/stock/{productId}` | Get stock levels |
| POST | `/stock/adjust` | Adjust stock |
| GET | `/stock/{productId}/movements` | Movement history |

## 🛠️ Tech Stack

| Layer | Technology |
|-------|------------|
| **Orchestration** | .NET Aspire 13.1 |
| **API** | ASP.NET Core Minimal APIs |
| **Gateway** | YARP (containerized) |
| **Database** | PostgreSQL + EF Core 10 |
| **Caching** | Redis |
| **CQRS** | MediatR 12.5.0 |
| **Validation** | FluentValidation 11.11 |
| **Testing** | TUnit + Aspire.Hosting.Testing |
| **Observability** | OpenTelemetry |

## 📚 Documentation

- [ROADMAP.md](docs/ROADMAP.md) - Full project roadmap with all phases
- [Architecture](docs/architecture.md) - System design decisions
- [WMS Concepts](docs/wms-concepts.md) - Warehouse management terminology

## 🗺️ Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| 1. Foundation | ✅ | Aspire + PostgreSQL + Redis |
| 2. YARP Gateway | ✅ | API Gateway with path transforms |
| 2.5 Testing | ✅ | TUnit + Aspire.Hosting.Testing |
| 3. Modular Monolith | ✅ | MediatR, FluentValidation, IModule pattern |
| 4. Inventory Module | ✅ | Products, Locations, Stock |
| 5. Inbound Module | 🔲 | Purchase Orders, Receiving |
| 6. Outbound Module | 🔲 | Orders, Picking, Shipping |
| 7. React Frontend | 🔲 | Dashboard with real-time updates |
| 8-11. Polish | 🔲 | Observability, Docs, CI/CD |

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

Built with ❤️ using [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
