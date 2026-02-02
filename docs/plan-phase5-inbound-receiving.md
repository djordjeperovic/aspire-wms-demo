# Phase 5 Inbound Module - Receiving Receipts

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan follows `.agents/PLANS.md` from the repo root and must be maintained in accordance with it.

## Purpose / Big Picture

This plan implements the receiving slice of Phase 5 "Inbound Module" from `docs/ROADMAP.md`. After this change, a user can record a receipt for an existing purchase order through the YARP gateway and see the purchase order status update to partially or fully received. The behavior is visible by calling the new inbound receipts endpoints and observing the updated purchase order status via the inbound purchase orders endpoints.

## Progress

- [x] (2026-02-02 14:20Z) Created plan and aligned it to `.agents/PLANS.md`.
- [x] (2026-02-02 15:10Z) Implemented inbound purchase orders, receipts, and receive logic in domain entities.
- [x] (2026-02-02 15:20Z) Added inbound EF Core context, configurations, seed data, and initial inbound migration.
- [x] (2026-02-02 15:35Z) Added purchase order and receipt endpoints, wired module, and added tests.
- [x] (2026-02-02 16:05Z) Re-ran functional tests after Docker was healthy.

## Surprises & Discoveries

- Observation: EF Core design-time migration failed when the inbound context referenced `Product` without a configuration.
  Evidence: `dotnet ef migrations add` raised a `Dimensions` constructor binding error until a product reference mapping excluded from migrations was added.
- Observation: Functional tests could not start due to Docker runtime health issues.
  Evidence: `Container runtime 'docker' was found but appears to be unhealthy` from Aspire.Hosting during `dotnet run --project tests/AspireWms.FunctionalTests`.
- Observation: Purchase order numbers are normalized to uppercase, which caused a case-sensitive test to fail.
  Evidence: Functional test compared the original mixed-case order number; response returned uppercased value.

## Decision Log

- Decision: Receipt creation will accept purchase order line ids and quantities instead of product ids.
  Rationale: It makes validation explicit and guarantees the receipt applies to the correct purchase order lines while supporting partial receipts.
  Date/Author: 2026-02-02 / Codex
- Decision: Receiving a draft purchase order is allowed and will transition it to partially or fully received.
  Rationale: The purchase order plan exposes Create but not Submit; this keeps the flow usable without adding a separate submit endpoint.
  Date/Author: 2026-02-02 / Codex
- Decision: Add an inbound-only `Product` mapping excluded from migrations to enable product foreign keys without duplicating inventory schema.
  Rationale: The inbound context needs product FKs, but inventory tables are managed by `InventoryDbContext`.
  Date/Author: 2026-02-02 / Codex

## Outcomes & Retrospective

Inbound purchase order and receipt flows are implemented with EF Core mappings, seed data, and tests. Unit and functional tests pass once Docker is running.

## Context and Orientation

This repo is a modular monolith API hosted by Aspire. The Inbound module lives under `src/AspireWms.Api/Modules/Inbound` and is routed through the YARP gateway from `/api/inbound/*` to `/inbound/*` as configured in `src/AspireWms.AppHost/AppHost.cs`. A "receipt" is the record of goods that actually arrived for a purchase order. A "receipt line" is a single row in that receipt describing the purchase order line it fulfills and the quantity received. A "purchase order line" is a row on the purchase order describing an expected product and quantity. The inbound purchase order slice is defined in `docs/plan-inbound-purchase-orders.md`; this plan assumes those entities, db context, and endpoints exist and describes the additional work needed for receiving.

The Inbound module uses EF Core with a shared PostgreSQL database and the `inbound` schema. The Inventory module and its seed data exist under `src/AspireWms.Api/Modules/Inventory` and provide the product data referenced by purchase orders. The API uses MediatR and the `Result<T>` pattern from `src/AspireWms.Api/Shared/Domain/Result.cs`.

## Plan of Work

Add receipt domain entities in `src/AspireWms.Api/Modules/Inbound/Domain/Entities` and update purchase order line logic so received quantities can be incremented safely. Define receipt creation rules in the aggregate root to prevent over-receiving and to update purchase order status. Keep the rules in domain methods so the API layer only orchestrates.

Extend the inbound EF Core model by adding receipt tables and relationships in `src/AspireWms.Api/Modules/Inbound/Infrastructure` and add a migration in the inbound migrations folder. Update the inbound seeder to include at least one sample receipt for a submitted or partially received purchase order so the list endpoint has data to show in functional tests.

Create receipt endpoints as a vertical slice in `src/AspireWms.Api/Modules/Inbound/Features/Receipts/ReceiptEndpoints.cs` using the same MediatR pattern as the Inventory module. Add a list endpoint with an optional purchase order filter, a get-by-id endpoint, and a create endpoint that validates purchase order and line references, applies receipt updates, saves, and invalidates cache keys for purchase orders and receipts.

Wire the new endpoints in `src/AspireWms.Api/Modules/Inbound/InboundModule.cs`. If `InboundDbContext` is not already registered (from the purchase order plan), add it there and ensure `Program.cs` applies inbound migrations and seed data. If `AppHost.cs` is touched, call out the required restart.

Add unit tests for receipt domain logic under `tests/AspireWms.UnitTests/Modules/Inbound/Domain/Entities` and functional tests under `tests/AspireWms.FunctionalTests` to verify receipt creation and listing through the gateway.

## Concrete Steps

From the repo root, start by running the apphost to confirm current resource health:

    aspire run

Create the inbound receipt migration after the new entities and configurations are in place:

    dotnet ef migrations add AddInboundReceipts --context InboundDbContext --output-dir Modules/Inbound/Infrastructure/Migrations --project src/AspireWms.Api

Run the unit and functional tests after implementation:

    dotnet run --project tests/AspireWms.UnitTests -c Release
    dotnet run --project tests/AspireWms.FunctionalTests -c Release

## Validation and Acceptance

Start the system with `aspire run`, then use the gateway to create a receipt and verify purchase order status. A successful receipt creation returns HTTP 201 and an id, and the purchase order should move to `PartiallyReceived` or `FullyReceived` depending on quantities. Listing receipts filtered by the purchase order id should include the new receipt.

Functional tests should include a test named `InboundReceiptEndpointsTests.CreateReceipt_UpdatesStatus` (or similar) that fails before the change and passes after. Run `dotnet run --project tests/AspireWms.FunctionalTests -c Release` and expect all tests to pass. Unit tests for receipt and purchase order line receive logic should pass in `tests/AspireWms.UnitTests`.

## Idempotence and Recovery

Most steps are repeatable. If the migration command is re-run, use a new migration name or delete the generated migration files before retrying. If seeding introduces duplicates, make the seeder check for existing receipts by purchase order number before inserting. If a receipt over-receives a line during testing, delete the database volume and rerun `aspire run` to re-seed from a clean state.

## Artifacts and Notes

Example request to create a receipt through the gateway:

    POST http://localhost:5000/api/inbound/receipts
    {
      "purchaseOrderId": "00000000-0000-0000-0000-000000000000",
      "receivedAt": "2026-02-02T14:30:00Z",
      "notes": "First delivery",
      "lines": [
        { "purchaseOrderLineId": "11111111-1111-1111-1111-111111111111", "quantityReceived": 5 }
      ]
    }

Example success response:

    HTTP/1.1 201 Created
    { "id": "22222222-2222-2222-2222-222222222222" }

## Interfaces and Dependencies

In `src/AspireWms.Api/Modules/Inbound/Domain/Entities/Receipt.cs`, define a receipt aggregate with an id, a purchase order id, received timestamp, notes, and a read-only collection of receipt lines. Provide a factory that validates non-empty lines and positive quantities, returning `Result<Receipt>`.

    public sealed class Receipt : Entity<Guid>
    {
        public Guid PurchaseOrderId { get; }
        public DateTime ReceivedAt { get; }
        public string? Notes { get; }
        public IReadOnlyCollection<ReceiptLine> Lines { get; }
        public static Result<Receipt> Create(Guid purchaseOrderId, DateTime receivedAt, string? notes, IEnumerable<ReceiptLine> lines);
    }

In `src/AspireWms.Api/Modules/Inbound/Domain/Entities/ReceiptLine.cs`, define a receipt line that references a purchase order line id and product id, plus received quantity and unit cost, using the `Quantity` and `Money` value objects:

    public sealed class ReceiptLine : Entity<Guid>
    {
        public Guid PurchaseOrderLineId { get; }
        public Guid ProductId { get; }
        public Quantity QuantityReceived { get; }
        public Money UnitCost { get; }
        public static Result<ReceiptLine> Create(Guid purchaseOrderLineId, Guid productId, Quantity quantityReceived, Money unitCost);
    }

In `src/AspireWms.Api/Modules/Inbound/Domain/Entities/PurchaseOrderLine.cs`, add a `Receive(Quantity quantityReceived)` method that increments `ReceivedQuantity` and rejects totals above ordered quantity. In `PurchaseOrder`, add a method such as `ApplyReceipt(IEnumerable<(Guid lineId, Quantity qty)> receivedLines)` that updates line totals and sets the status to `PartiallyReceived` or `FullyReceived` based on all line quantities.

In `src/AspireWms.Api/Modules/Inbound/Infrastructure/InboundDbContext.cs`, add `DbSet<Receipt>` and `DbSet<ReceiptLine>`. In `Configurations`, map `receipts` and `receipt_lines` tables in the `inbound` schema, set foreign keys to `purchase_orders` and `purchase_order_lines`, and configure value object conversions for `Quantity` and `Money`.

In `src/AspireWms.Api/Modules/Inbound/Features/Receipts/ReceiptEndpoints.cs`, define:

    public sealed record ReceiptLineRequest(Guid PurchaseOrderLineId, int QuantityReceived);
    public sealed record CreateReceiptCommand(Guid PurchaseOrderId, DateTime ReceivedAt, string? Notes, IReadOnlyList<ReceiptLineRequest> Lines)
        : IRequest<CreateReceiptResult>;
    public sealed record CreateReceiptResult(bool Success, Guid? Id = null, string? Error = null);

Expose endpoints:

    GET /inbound/receipts?purchaseOrderId={id}
    GET /inbound/receipts/{id}
    POST /inbound/receipts

The create handler must load the purchase order and its lines, validate all receipt line ids belong to that purchase order, enforce positive quantities, prevent duplicates within the same receipt, apply the receipt updates, and persist in a single save. Use `IDistributedCache` to invalidate purchase order list/get caches and any receipt list caches.

Plan update note (2026-02-02): Marked functional test re-run complete, captured the order number normalization test fix, and updated the retrospective to note passing validation.
