# Aspire WMS - Project Roadmap

> **Cloud-native Warehouse Management System** built with .NET Aspire, demonstrating Modular Monolith architecture with YARP API Gateway.

---

## 🏛️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      Aspire AppHost                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐         ┌─────────────────────────────────┐   │
│  │   React     │         │        YARP API Gateway         │   │
│  │  Frontend   │────────▶│  (Containerized)                │   │
│  │             │         │  • Route: /api/inventory/*      │   │
│  └─────────────┘         │  • Route: /api/inbound/*        │   │
│                          │  • Route: /api/outbound/*       │   │
│                          └──────────────┬──────────────────┘   │
│                                         │                       │
│                                         ▼                       │
│              ┌──────────────────────────────────────────┐      │
│              │     Modular Monolith API                 │      │
│              │     (AspireWms.Api)                      │      │
│              │  ┌────────────┬────────────┬──────────┐  │      │
│              │  │ Inventory  │  Inbound   │ Outbound │  │      │
│              │  │  Module    │  Module    │  Module  │  │      │
│              │  └────────────┴────────────┴──────────┘  │      │
│              └──────────────────────────────────────────┘      │
│                          │                                      │
│              ┌───────────┴───────────┐                         │
│              │                       │                         │
│       ┌──────▼──────┐         ┌──────▼──────┐                  │
│       │  PostgreSQL │         │    Redis    │                  │
│       │  (wmsdb)    │         │   (cache)   │                  │
│       └─────────────┘         └─────────────┘                  │
└─────────────────────────────────────────────────────────────────┘
```

### Database Schema Strategy

All modules share a **single PostgreSQL database** (`wmsdb`) with **separate schemas** per module:

| Module | Schema | Tables |
|--------|--------|--------|
| Inventory | `inventory` | products, locations, inventory_items, stock_movements |
| Inbound | `inbound` | purchase_orders, purchase_order_lines, receipts, receipt_lines |
| Outbound | `outbound` | orders, order_lines, pick_tasks, shipments |

**Benefits:**
- **Cross-module queries** via PostgreSQL cross-schema joins (when needed)
- **Single connection string** simplifies configuration
- **Logical isolation** - each module owns its schema
- **Easy microservices extraction** - split schemas into separate databases later

**Pattern:**
```csharp
// Each module DbContext sets its own schema
modelBuilder.HasDefaultSchema("inventory");
npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "inventory");
```

---

## 📊 Progress Summary

| Phase | Status | Description |
|-------|--------|-------------|
| **Phase 1** | ✅ Complete | Foundation & Infrastructure |
| **Phase 2** | ✅ Complete | YARP API Gateway |
| **Phase 2.5** | ✅ Complete | Testing Infrastructure |
| **Phase 3** | ✅ Complete | Modular Monolith Structure |
| **Phase 4** | ✅ Complete | Inventory Module |
| **Phase 5** | 🔲 Next | Inbound Module |
| **Phase 6** | 🔲 Planned | Outbound Module |
| **Phase 7** | 🔲 Planned | React Frontend |
| **Phase 8** | 🔲 Planned | Observability |
| **Phase 9** | 🔲 Planned | Documentation |
| **Phase 10** | 🔲 Planned | CI/CD |
| **Phase 11** | 🔲 Planned | Polish & Demo Data |

---

## ✅ Phase 1: Foundation & Infrastructure — COMPLETE

### Aspire Orchestration
- [x] Aspire AppHost project setup (.NET 10, Aspire 13.1)
- [x] Service discovery configuration
- [x] Aspire Dashboard integration

### Infrastructure Resources
- [x] PostgreSQL database container with data volume
- [x] PgAdmin for database management
- [x] Redis cache container with data volume
- [x] Redis Commander for cache inspection
- [x] Health checks for all resources

### Service Defaults
- [x] OpenTelemetry configuration (traces, metrics, logs)
- [x] Default health check endpoints (`/health`, `/alive`)
- [x] Resilience policies (retry, circuit breaker)
- [x] Service discovery extensions

---

## ✅ Phase 2: YARP API Gateway — COMPLETE

### Gateway Configuration
- [x] YARP reverse proxy via `Aspire.Hosting.Yarp` (containerized)
- [x] Host port configuration (5000)
- [x] Dependency ordering with `.WaitFor(api)`

### Route Configuration with Path Transforms
```
/api/inventory/*  → API → /inventory/*  (strips /api prefix)
/api/inbound/*    → API → /inbound/*    (strips /api prefix)
/api/outbound/*   → API → /outbound/*   (strips /api prefix)
/api              → API → /api          (root endpoint)
```

---

## ✅ Phase 2.5: Testing Infrastructure — COMPLETE

### Test Framework Stack
| Layer | Package | Purpose |
|-------|---------|---------|
| Test Framework | **TUnit** | Modern test runner, `[Test]`, `Assert.That()` |
| Aspire Testing | **Aspire.Hosting.Testing** | `DistributedApplicationTestingBuilder` |
| Resilience | **Polly** | Retry policies for container startup |

### Test Projects (2)
```
tests/
├── AspireWms.UnitTests/           # 6 tests ✅
│   └── Modules/Inventory/
│       └── SampleUnitTests.cs     # Parameterized tests with [Arguments]
│
└── AspireWms.FunctionalTests/     # 5 tests ✅
    ├── Fixtures/
    │   └── AspireAppFixture.cs    # DistributedApplicationTestingBuilder
    └── GatewayTests.cs            # YARP routing verification
```

### Test Commands
```bash
# Unit tests (fast, no Docker)
dotnet run --project tests/AspireWms.UnitTests -c Release

# Functional tests (requires Docker)
dotnet run --project tests/AspireWms.FunctionalTests -c Release
```

### Key Patterns Implemented
- `[Before(Class)]` / `[After(Class)]` for fixture lifecycle
- `WaitForResourceHealthyAsync()` for container readiness
- Polly retry for container startup race conditions
- `Assert.That().IsEqualTo()` async assertions

---

## ✅ Phase 3: Modular Monolith Structure — COMPLETE

### Packages
| Package | Version | Purpose |
|---------|---------|---------|
| **MediatR** | 12.5.0 | CQRS pattern (last free MIT version) |
| **FluentValidation** | 11.11.0 | Request validation |

### Vertical Slice Architecture
```
AspireWms.Api/
├── Shared/
│   ├── Domain/
│   │   ├── Result.cs              # Result<T> pattern with Error
│   │   └── ValueObjects/
│   │       ├── Quantity.cs        # Non-negative quantity
│   │       └── Money.cs           # Currency-aware monetary value
│   ├── Infrastructure/
│   │   ├── DependencyInjection.cs # AddSharedInfrastructure()
│   │   └── Behaviors/
│   │       ├── ValidationBehavior.cs
│   │       └── LoggingBehavior.cs
│   └── Contracts/
│       └── IModule.cs             # Module registration interface
├── Modules/
│   ├── Inventory/
│   │   └── InventoryModule.cs     # /inventory/* endpoints
│   ├── Inbound/
│   │   └── InboundModule.cs       # /inbound/* endpoints
│   └── Outbound/
│       └── OutboundModule.cs      # /outbound/* endpoints
└── Program.cs                     # Wires up all modules
```

### Key Patterns
- **Result<T>** - Success/Failure with Error codes
- **Value Objects** - Quantity, Money with validation
- **MediatR Behaviors** - Validation + Logging pipeline
- **IModule** - Static interface for module registration

### Test Coverage
- Unit Tests: **44 tests** (Result, Quantity, Money)
- Functional Tests: **5 tests** (Gateway routing)

---

## ✅ Phase 4: Inventory Module — COMPLETE

### Domain Models
- [x] `Product` - SKU, name, description, dimensions, weight
- [x] `Location` - Zone-Aisle-Rack-Bin code format (e.g., "A-01-02-03")
- [x] `InventoryItem` - Product + Location + Quantity
- [x] `StockMovement` - Audit trail with MovementType and reason

### Features (Vertical Slices)
- [x] Products: List, Get, Create, Update (with Redis caching)
- [x] Locations: List (with zone filter), Create
- [x] Stock: Get levels, Adjust, Movement history

### Infrastructure
- [x] `InventoryDbContext` with EF Core and Npgsql
- [x] Entity configurations (Product, Location, InventoryItem, StockMovement)
- [x] Initial migration with schema `inventory`
- [x] Auto-migration on startup
- [x] Redis caching (5min list, 10min individual products)

### Seed Data
- 10 products (electronics/accessories)
- 40 locations (Zone A: Receiving, Zone B: Storage, Zone C: Shipping)
- Distributed inventory with initial stock movements

### Test Coverage
- Unit Tests: **112 tests** (68 new domain entity tests)
- Functional Tests: **15 tests** (10 new inventory endpoint tests)

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/inventory/products` | List products |
| GET | `/inventory/products/{id}` | Get product by ID |
| POST | `/inventory/products` | Create product |
| PUT | `/inventory/products/{id}` | Update product |
| GET | `/inventory/locations` | List locations |
| POST | `/inventory/locations` | Create location |
| GET | `/inventory/stock/{productId}` | Get stock levels |
| POST | `/inventory/stock/adjust` | Adjust stock |
| GET | `/inventory/stock/{productId}/movements` | Movement history |

---

## 🔲 Phase 5: Inbound Module

### Domain Models
- [ ] `PurchaseOrder` - Expected shipments
- [ ] `PurchaseOrderLine` - Expected items
- [ ] `Receipt` - Actual received goods
- [ ] `ReceiptLine` - Received items

### Features
- [ ] Purchase Orders: List, Create, Get by ID
- [ ] Receiving: Receive goods (full/partial)
- [ ] Put-away: Assign locations

---

## 🔲 Phase 6: Outbound Module

### Domain Models
- [ ] `Order` - Customer order
- [ ] `OrderLine` - Ordered items
- [ ] `PickTask` - Warehouse picker assignment
- [ ] `Shipment` - Packed and shipped

### Features
- [ ] Orders: List, Create, Get by ID
- [ ] Picking: Get tasks, Confirm pick
- [ ] Shipping: Pack, Ship with tracking

---

## 🔲 Phase 7: React Frontend

### Setup
- [ ] Vite + React + TypeScript
- [ ] Tailwind CSS
- [ ] React Router
- [ ] API client → YARP Gateway

### Pages
- [ ] Dashboard with KPIs
- [ ] Inventory Management (Products, Stock, Locations)
- [ ] Inbound (Purchase Orders, Receiving)
- [ ] Outbound (Orders, Picking, Shipping)
- [ ] Analytics Dashboard

---

## 🔲 Phase 8: Observability

- [ ] Distributed tracing (Gateway → API → DB)
- [ ] Custom metrics (orders/hour, pick time)
- [ ] Structured logging with Serilog
- [ ] Correlation ID propagation

---

## 🔲 Phase 9: Documentation

- [ ] Updated README with screenshots
- [ ] Architecture deep-dive
- [ ] API reference
- [ ] Deployment guide

---

## 🔲 Phase 10: CI/CD

- [ ] GitHub Actions: Build + Test
- [ ] Code coverage reporting
- [ ] Docker image builds
- [ ] GitHub Container Registry

---

## 🔲 Phase 11: Polish

- [ ] Realistic seed data (100+ products)
- [ ] Sample orders in various states
- [ ] Docker Compose alternative
- [ ] VS Code launch configs

---

## 🚀 Future: Microservices Migration

When ready to split the monolith:

1. Extract module to separate project (`AspireWms.Inventory.Api`)
2. Update YARP routes to point to new service
3. Add message bus (RabbitMQ) for events
4. No frontend changes needed (same API contracts)

---

## 📁 Current Project Structure

```
aspire-wms/
├── src/
│   ├── AspireWms.AppHost/           # Aspire orchestration ✅
│   ├── AspireWms.ServiceDefaults/   # Shared config ✅
│   └── AspireWms.Api/               # Modular monolith API ✅
├── tests/
│   ├── AspireWms.UnitTests/         # TUnit tests ✅
│   └── AspireWms.FunctionalTests/   # Aspire integration tests ✅
├── docs/
│   ├── ROADMAP.md                   # This file ✅
│   ├── architecture.md
│   ├── aspire-overview.md
│   └── wms-concepts.md
└── AspireWms.sln
```

---

## 🛠️ Tech Stack

| Category | Technology |
|----------|------------|
| Orchestration | .NET Aspire 13.1 |
| Runtime | .NET 10 |
| API Gateway | YARP (containerized) |
| Backend | ASP.NET Core Minimal APIs |
| Database | PostgreSQL |
| Cache | Redis |
| Testing | TUnit + Aspire.Hosting.Testing |
| Frontend | React + TypeScript (planned) |
