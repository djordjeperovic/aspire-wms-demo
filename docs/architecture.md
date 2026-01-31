# System Architecture

This document describes the architecture of the Aspire WMS system.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Aspire AppHost                             │
│                   (Orchestration Layer)                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │   React     │    │  Inventory  │    │ Fulfillment │         │
│  │  Frontend   │───▶│     API     │◀──▶│   Service   │         │
│  │  (Web SPA)  │    │             │    │             │         │
│  └─────────────┘    └──────┬──────┘    └──────┬──────┘         │
│                            │                   │                │
│                     ┌──────┴───────────────────┴──────┐        │
│                     │                                  │        │
│              ┌──────▼──────┐              ┌───────────▼───┐    │
│              │  PostgreSQL │              │     Redis     │    │
│              │  (inventorydb)│            │    (cache)    │    │
│              └─────────────┘              └───────────────┘    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Services

### Inventory API (`AspireWms.Inventory.Api`)

**Responsibility**: Core inventory management

- Product catalog management
- Stock level tracking
- Location hierarchy management
- Stock movement recording

**Dependencies**:
- PostgreSQL (inventorydb)
- Redis (caching)

### Inbound API (`AspireWms.Inbound.Api`)

**Responsibility**: Receiving operations

- Purchase order management
- Goods receiving
- Put-away assignments
- Supplier management

**Dependencies**:
- PostgreSQL (inventorydb)
- Inventory API (service-to-service)

### Outbound API (`AspireWms.Outbound.Api`)

**Responsibility**: Order fulfillment

- Customer order processing
- Pick task generation
- Packing workflows
- Shipping confirmation

**Dependencies**:
- PostgreSQL (inventorydb)
- Inventory API (stock reservation)
- Redis (task queuing)

### React Frontend (`AspireWms.Web`)

**Responsibility**: User interface

- Dashboard with KPIs
- Inventory management UI
- Inbound/Outbound workflows
- Analytics charts

## Data Flow

### Inbound Flow
```
Supplier → Purchase Order → Receive Goods → Put-away → Inventory Updated
```

### Outbound Flow
```
Customer Order → Pick Task → Pick Confirmation → Pack → Ship → Inventory Reduced
```

## Technology Decisions

| Decision | Rationale |
|----------|-----------|
| **PostgreSQL** | Robust RDBMS with excellent EF Core support |
| **Redis** | Fast caching and pub/sub for real-time features |
| **Separate APIs** | Clear domain boundaries, independent scaling |
| **React + Vite** | Modern, fast development experience |

## Communication Patterns

### Synchronous (HTTP)
- Frontend → APIs (REST)
- API → API (service discovery)

### Asynchronous
- Redis pub/sub for inventory events
- Background processing for reports

## Security Considerations

- All services run in isolated containers
- Connection strings managed via Aspire secrets
- Health endpoints protected in production
