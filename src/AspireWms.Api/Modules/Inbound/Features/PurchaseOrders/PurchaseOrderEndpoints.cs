using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Modules.Inbound.Infrastructure;
using AspireWms.Api.Modules.Inventory.Infrastructure;
using AspireWms.Api.Shared.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AspireWms.Api.Modules.Inbound.Features.PurchaseOrders;

// === DTOs ===
public sealed record PurchaseOrderLineDto(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    decimal UnitCostAmount,
    string UnitCostCurrency,
    decimal ReceivedQuantity);

public sealed record PurchaseOrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string SupplierName,
    string Status,
    DateOnly? ExpectedDeliveryDate,
    int LineCount);

public sealed record PurchaseOrderDto(
    Guid Id,
    string OrderNumber,
    string SupplierName,
    string Status,
    DateOnly? ExpectedDeliveryDate,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

// === List Purchase Orders ===
public sealed record ListPurchaseOrdersQuery(PurchaseOrderStatus? Status) : IRequest<IReadOnlyList<PurchaseOrderSummaryDto>>;

public sealed class ListPurchaseOrdersHandler(InboundDbContext db, IDistributedCache cache)
    : IRequestHandler<ListPurchaseOrdersQuery, IReadOnlyList<PurchaseOrderSummaryDto>>
{
    private static string GetCacheKey(PurchaseOrderStatus? status) =>
        $"inbound:purchase-orders:list:{status?.ToString() ?? "all"}";

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<IReadOnlyList<PurchaseOrderSummaryDto>> Handle(
        ListPurchaseOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(request.Status);
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<List<PurchaseOrderSummaryDto>>(cached) ?? [];
        }

        var query = db.PurchaseOrders.AsNoTracking();
        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        var orders = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PurchaseOrderSummaryDto(
                p.Id,
                p.OrderNumber,
                p.SupplierName,
                p.Status.ToString(),
                p.ExpectedDeliveryDate,
                p.Lines.Count))
            .ToListAsync(cancellationToken);

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(orders),
            CacheOptions,
            cancellationToken);

        return orders;
    }
}

// === Get Purchase Order ===
public sealed record GetPurchaseOrderQuery(Guid Id) : IRequest<PurchaseOrderDto?>;

public sealed class GetPurchaseOrderHandler(InboundDbContext db, IDistributedCache cache)
    : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderDto?>
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    public async Task<PurchaseOrderDto?> Handle(GetPurchaseOrderQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"inbound:purchase-orders:{request.Id}";
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<PurchaseOrderDto>(cached);
        }

        var order = await db.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Lines)
            .Where(p => p.Id == request.Id)
            .Select(p => new PurchaseOrderDto(
                p.Id,
                p.OrderNumber,
                p.SupplierName,
                p.Status.ToString(),
                p.ExpectedDeliveryDate,
                p.Notes,
                p.CreatedAt,
                p.UpdatedAt,
                p.Lines
                    .OrderBy(l => l.CreatedAt)
                    .Select(l => new PurchaseOrderLineDto(
                        l.Id,
                        l.ProductId,
                        l.Quantity.Value,
                        l.UnitCost.Amount,
                        l.UnitCost.Currency,
                        l.ReceivedQuantity.Value))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (order is not null)
        {
            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(order),
                CacheOptions,
                cancellationToken);
        }

        return order;
    }
}

// === Create Purchase Order ===
public sealed record CreatePurchaseOrderLineRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitCostAmount,
    string? UnitCostCurrency);

public sealed record CreatePurchaseOrderCommand(
    string OrderNumber,
    string SupplierName,
    DateOnly? ExpectedDeliveryDate,
    string? Notes,
    IReadOnlyList<CreatePurchaseOrderLineRequest> Lines)
    : IRequest<CreatePurchaseOrderResult>;

public sealed record CreatePurchaseOrderResult(bool Success, Guid? Id = null, string? Error = null);

public sealed class CreatePurchaseOrderHandler(
    InboundDbContext db,
    InventoryDbContext inventoryDb,
    IDistributedCache cache)
    : IRequestHandler<CreatePurchaseOrderCommand, CreatePurchaseOrderResult>
{
    public async Task<CreatePurchaseOrderResult> Handle(
        CreatePurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
            return new CreatePurchaseOrderResult(false, Error: "At least one line is required.");

        var normalizedOrderNumber = request.OrderNumber.Trim().ToUpperInvariant();
        var orderExists = await db.PurchaseOrders
            .AnyAsync(p => p.OrderNumber == normalizedOrderNumber, cancellationToken);

        if (orderExists)
            return new CreatePurchaseOrderResult(false, Error: $"Order number '{normalizedOrderNumber}' already exists.");

        if (request.Lines.GroupBy(l => l.ProductId).Any(g => g.Count() > 1))
            return new CreatePurchaseOrderResult(false, Error: "Duplicate products are not allowed.");

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existingProductIds = await inventoryDb.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (existingProductIds.Count != productIds.Count)
            return new CreatePurchaseOrderResult(false, Error: "One or more products were not found.");

        var orderResult = PurchaseOrder.Create(
            request.OrderNumber,
            request.SupplierName,
            request.ExpectedDeliveryDate,
            request.Notes);

        if (orderResult.IsFailure)
            return new CreatePurchaseOrderResult(false, Error: orderResult.Error.Message);

        foreach (var line in request.Lines)
        {
            var quantityResult = Quantity.Create(line.Quantity);
            if (quantityResult.IsFailure)
                return new CreatePurchaseOrderResult(false, Error: quantityResult.Error.Message);

            var unitCostResult = Money.Create(line.UnitCostAmount, line.UnitCostCurrency ?? "USD");
            if (unitCostResult.IsFailure)
                return new CreatePurchaseOrderResult(false, Error: unitCostResult.Error.Message);

            var addResult = orderResult.Value.AddLine(
                line.ProductId,
                quantityResult.Value,
                unitCostResult.Value);

            if (addResult.IsFailure)
                return new CreatePurchaseOrderResult(false, Error: addResult.Error.Message);
        }

        db.PurchaseOrders.Add(orderResult.Value);
        await db.SaveChangesAsync(cancellationToken);

        await CacheInvalidation.InvalidatePurchaseOrderCaches(cache, cancellationToken);

        return new CreatePurchaseOrderResult(true, orderResult.Value.Id);
    }
}

internal static class CacheInvalidation
{
    public static async Task InvalidatePurchaseOrderCaches(
        IDistributedCache cache,
        CancellationToken cancellationToken)
    {
        await cache.RemoveAsync("inbound:purchase-orders:list:all", cancellationToken);

        foreach (var status in Enum.GetValues<PurchaseOrderStatus>())
        {
            await cache.RemoveAsync(
                $"inbound:purchase-orders:list:{status}",
                cancellationToken);
        }
    }
}

// === Endpoints ===
public static class PurchaseOrderEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        var purchaseOrders = group.MapGroup("/purchase-orders").WithTags("PurchaseOrders");

        purchaseOrders.MapGet("/", async (PurchaseOrderStatus? status, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListPurchaseOrdersQuery(status));
            return Results.Ok(result);
        })
        .WithName("ListPurchaseOrders")
        .WithSummary("List purchase orders (optional status filter)");

        purchaseOrders.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPurchaseOrderQuery(id));
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { error = "Purchase order not found." });
        })
        .WithName("GetPurchaseOrder")
        .WithSummary("Get a purchase order by ID");

        purchaseOrders.MapPost("/", async (CreatePurchaseOrderCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.Success
                ? Results.Created($"/inbound/purchase-orders/{result.Id}", new { id = result.Id })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CreatePurchaseOrder")
        .WithSummary("Create a new purchase order");
    }
}
