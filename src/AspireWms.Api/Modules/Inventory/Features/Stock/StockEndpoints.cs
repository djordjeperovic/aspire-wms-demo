using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Domain.Enums;
using AspireWms.Api.Modules.Inventory.Infrastructure;
using AspireWms.Api.Shared.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AspireWms.Api.Modules.Inventory.Features.Stock;

// === DTOs ===
public sealed record StockLevelDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    IReadOnlyList<LocationStockDto> Locations,
    decimal TotalQuantity);

public sealed record LocationStockDto(
    Guid LocationId,
    string LocationCode,
    decimal Quantity);

public sealed record StockMovementDto(
    Guid Id,
    string MovementType,
    decimal Quantity,
    string Reason,
    DateTime CreatedAt);

// === Get Stock Level ===
public sealed record GetStockLevelQuery(Guid ProductId) : IRequest<StockLevelDto?>;

public sealed class GetStockLevelHandler(InventoryDbContext db)
    : IRequestHandler<GetStockLevelQuery, StockLevelDto?>
{
    public async Task<StockLevelDto?> Handle(GetStockLevelQuery request, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .Where(p => p.Id == request.ProductId)
            .Select(p => new { p.Id, p.Sku, p.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return null;

        var inventoryItems = await db.InventoryItems
            .Where(i => i.ProductId == request.ProductId)
            .Include(i => i.Location)
            .ToListAsync(cancellationToken);

        var locations = inventoryItems
            .Select(i => new LocationStockDto(i.LocationId, i.Location.Code, i.Quantity.Value))
            .OrderBy(l => l.LocationCode)
            .ToList();

        var totalQuantity = inventoryItems.Sum(i => i.Quantity.Value);

        return new StockLevelDto(
            product.Id,
            product.Sku,
            product.Name,
            locations,
            totalQuantity);
    }
}

// === Adjust Stock ===
public sealed record AdjustStockCommand(
    Guid ProductId,
    Guid LocationId,
    int Adjustment,
    string Reason) : IRequest<AdjustStockResult>;

public sealed record AdjustStockResult(bool Success, decimal? NewQuantity = null, string? Error = null);

public sealed class AdjustStockHandler(InventoryDbContext db)
    : IRequestHandler<AdjustStockCommand, AdjustStockResult>
{
    public async Task<AdjustStockResult> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        if (request.Adjustment == 0)
        {
            return new AdjustStockResult(false, Error: "Adjustment cannot be zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return new AdjustStockResult(false, Error: "Reason is required for stock adjustments.");
        }

        // Find or create inventory item
        var inventoryItem = await db.InventoryItems
            .Include(i => i.Movements)
            .FirstOrDefaultAsync(
                i => i.ProductId == request.ProductId && i.LocationId == request.LocationId,
                cancellationToken);

        if (inventoryItem is null)
        {
            // Create new inventory item if adjustment is positive
            if (request.Adjustment < 0)
            {
                return new AdjustStockResult(false, Error: "Cannot reduce stock for non-existent inventory item.");
            }

            // Verify product and location exist
            var productExists = await db.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);
            if (!productExists)
            {
                return new AdjustStockResult(false, Error: "Product not found.");
            }

            var locationExists = await db.Locations.AnyAsync(l => l.Id == request.LocationId, cancellationToken);
            if (!locationExists)
            {
                return new AdjustStockResult(false, Error: "Location not found.");
            }

            var quantityResult = Quantity.Create(request.Adjustment);
            if (quantityResult.IsFailure)
            {
                return new AdjustStockResult(false, Error: quantityResult.Error.Message);
            }

            var newItemResult = InventoryItem.Create(
                request.ProductId,
                request.LocationId,
                quantityResult.Value,
                request.Reason);

            if (newItemResult.IsFailure)
            {
                return new AdjustStockResult(false, Error: newItemResult.Error.Message);
            }

            db.InventoryItems.Add(newItemResult.Value);
            await db.SaveChangesAsync(cancellationToken);

            return new AdjustStockResult(true, newItemResult.Value.Quantity.Value);
        }

        // Adjust existing inventory
        var adjustResult = inventoryItem.AdjustStock(request.Adjustment, request.Reason);
        if (adjustResult.IsFailure)
        {
            return new AdjustStockResult(false, Error: adjustResult.Error.Message);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AdjustStockResult(true, inventoryItem.Quantity.Value);
    }
}

// === Get Movement History ===
public sealed record GetMovementHistoryQuery(Guid ProductId, int Limit = 50) : IRequest<IReadOnlyList<StockMovementDto>>;

public sealed class GetMovementHistoryHandler(InventoryDbContext db)
    : IRequestHandler<GetMovementHistoryQuery, IReadOnlyList<StockMovementDto>>
{
    public async Task<IReadOnlyList<StockMovementDto>> Handle(
        GetMovementHistoryQuery request, 
        CancellationToken cancellationToken)
    {
        var inventoryItemIds = await db.InventoryItems
            .Where(i => i.ProductId == request.ProductId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        if (inventoryItemIds.Count == 0)
            return [];

        return await db.StockMovements
            .Where(m => inventoryItemIds.Contains(m.InventoryItemId))
            .OrderByDescending(m => m.CreatedAt)
            .Take(request.Limit)
            .Select(m => new StockMovementDto(
                m.Id,
                m.MovementType.ToString(),
                m.Quantity.Value,
                m.Reason,
                m.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// === Endpoints ===
public static class StockEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        var stock = group.MapGroup("/stock").WithTags("Stock");

        stock.MapGet("/{productId:guid}", async (Guid productId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetStockLevelQuery(productId));
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { error = "Product not found or has no inventory." });
        })
        .WithName("GetStockLevel")
        .WithSummary("Get stock levels for a product across all locations");

        stock.MapPost("/adjust", async (AdjustStockCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.Success
                ? Results.Ok(new { newQuantity = result.NewQuantity })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("AdjustStock")
        .WithSummary("Adjust stock quantity for a product at a location");

        stock.MapGet("/{productId:guid}/movements", async (Guid productId, IMediator mediator, int limit = 50) =>
        {
            var result = await mediator.Send(new GetMovementHistoryQuery(productId, limit));
            return Results.Ok(result);
        })
        .WithName("GetMovementHistory")
        .WithSummary("Get stock movement history for a product");
    }
}
