# Plan: Inbound Module — Purchase Orders (List, Create, Get)

## Prerequisite: Move `Entity<TId>` to Shared

The base class is in `Modules/Inventory/Domain/Entities/Entity.cs`. Move it to `Shared/Domain/Entity.cs` and update namespace. Fix imports in all Inventory entities (Product, Location, InventoryItem, StockMovement).

## New Files to Create

### Domain (3 files)
- `Modules/Inbound/Domain/Entities/PurchaseOrderStatus.cs` — Enum: Draft, Submitted, PartiallyReceived, FullyReceived, Cancelled
- `Modules/Inbound/Domain/Entities/PurchaseOrderLine.cs` — Entity with ProductId (Guid), Quantity, UnitCost (Money), ReceivedQuantity (starts at zero). Factory `Create()` returning `Result<PurchaseOrderLine>`
- `Modules/Inbound/Domain/Entities/PurchaseOrder.cs` — Aggregate root with OrderNumber (unique), SupplierName, Status, ExpectedDeliveryDate (DateOnly?), Notes, Lines collection. Methods: `Create()`, `AddLine()`, `Submit()`, `Cancel()` — all returning Result

### Infrastructure (4 files + migration)
- `Modules/Inbound/Infrastructure/InboundDbContext.cs` — Schema "inbound", DbSets for PurchaseOrders and PurchaseOrderLines
- `Modules/Inbound/Infrastructure/Configurations/PurchaseOrderConfiguration.cs` — Table "purchase_orders", unique index on order_number, snake_case columns
- `Modules/Inbound/Infrastructure/Configurations/PurchaseOrderLineConfiguration.cs` — Table "purchase_order_lines", value object conversions for Quantity/Money, FK to purchase_orders (cascade) and inventory.products
- `Modules/Inbound/Infrastructure/InboundDbSeeder.cs` — 3 sample POs (Draft, Submitted, Cancelled) with lines referencing seeded Inventory products (query by SKU to get IDs)

### Feature (1 file — vertical slice)
- `Modules/Inbound/Features/PurchaseOrders/PurchaseOrderEndpoints.cs` — Contains:
  - DTOs: `PurchaseOrderDto`, `PurchaseOrderLineDto`
  - `ListPurchaseOrdersQuery(status?)` + Handler (with Redis caching)
  - `GetPurchaseOrderQuery(id)` + Handler (with Redis caching, includes lines)
  - `CreatePurchaseOrderCommand` + Handler (validates unique OrderNumber, validates ProductIds exist via InventoryDbContext query, creates PO + lines, invalidates cache)
  - Static `PurchaseOrderEndpoints.Map()` wiring GET/GET/{id}/POST

### Tests
- `tests/AspireWms.UnitTests/Modules/Inbound/Domain/Entities/PurchaseOrderTests.cs` — Factory validation, AddLine, Submit/Cancel state transitions
- `tests/AspireWms.UnitTests/Modules/Inbound/Domain/Entities/PurchaseOrderLineTests.cs` — Factory validation, value objects
- `tests/AspireWms.FunctionalTests/InboundEndpointsTests.cs` — List, Get, Create through YARP gateway at `/api/inbound/purchase-orders`

## Files to Modify

- `Modules/Inventory/Domain/Entities/Product.cs` — Update Entity import to Shared.Domain
- `Modules/Inventory/Domain/Entities/Location.cs` — Same
- `Modules/Inventory/Domain/Entities/InventoryItem.cs` — Same
- `Modules/Inventory/Domain/Entities/StockMovement.cs` — Same
- `Modules/Inbound/InboundModule.cs` — Add InboundDbContext + Redis registration in `RegisterServices`, add `PurchaseOrderEndpoints.Map(group)` in `MapEndpoints`
- `Program.cs` — Add InboundDbContext migration + seed block (after Inventory block, line ~29)

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/inbound/purchase-orders?status={status}` | List POs, optional status filter |
| GET | `/inbound/purchase-orders/{id}` | Get PO with lines |
| POST | `/inbound/purchase-orders` | Create PO with lines |

## Migration Command
```bash
dotnet ef migrations add InitialInboundSchema --context InboundDbContext --output-dir Modules/Inbound/Infrastructure/Migrations --project src/AspireWms.Api
```

## Verification
```bash
dotnet build AspireWms.sln
dotnet run --project tests/AspireWms.UnitTests -c Release
dotnet run --project tests/AspireWms.FunctionalTests -c Release
```
