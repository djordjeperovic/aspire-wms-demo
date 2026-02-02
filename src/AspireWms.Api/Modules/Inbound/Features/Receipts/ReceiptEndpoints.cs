using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Modules.Inbound.Features.PurchaseOrders;
using AspireWms.Api.Modules.Inbound.Infrastructure;
using AspireWms.Api.Shared.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AspireWms.Api.Modules.Inbound.Features.Receipts;

// === DTOs ===
public sealed record ReceiptLineDto(
    Guid Id,
    Guid PurchaseOrderLineId,
    Guid ProductId,
    decimal QuantityReceived,
    decimal UnitCostAmount,
    string UnitCostCurrency);

public sealed record ReceiptSummaryDto(
    Guid Id,
    Guid PurchaseOrderId,
    DateTime ReceivedAt,
    int LineCount);

public sealed record ReceiptDto(
    Guid Id,
    Guid PurchaseOrderId,
    DateTime ReceivedAt,
    string? Notes,
    IReadOnlyList<ReceiptLineDto> Lines);

// === List Receipts ===
public sealed record ListReceiptsQuery(Guid? PurchaseOrderId) : IRequest<IReadOnlyList<ReceiptSummaryDto>>;

public sealed class ListReceiptsHandler(InboundDbContext db, IDistributedCache cache)
    : IRequestHandler<ListReceiptsQuery, IReadOnlyList<ReceiptSummaryDto>>
{
    private static string GetCacheKey(Guid? purchaseOrderId) =>
        $"inbound:receipts:list:{purchaseOrderId?.ToString() ?? "all"}";

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<IReadOnlyList<ReceiptSummaryDto>> Handle(
        ListReceiptsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(request.PurchaseOrderId);
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<List<ReceiptSummaryDto>>(cached) ?? [];
        }

        var query = db.Receipts.AsNoTracking();
        if (request.PurchaseOrderId.HasValue)
            query = query.Where(r => r.PurchaseOrderId == request.PurchaseOrderId.Value);

        var receipts = await query
            .OrderByDescending(r => r.ReceivedAt)
            .Select(r => new ReceiptSummaryDto(
                r.Id,
                r.PurchaseOrderId,
                r.ReceivedAt,
                r.Lines.Count))
            .ToListAsync(cancellationToken);

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(receipts),
            CacheOptions,
            cancellationToken);

        return receipts;
    }
}

// === Get Receipt ===
public sealed record GetReceiptQuery(Guid Id) : IRequest<ReceiptDto?>;

public sealed class GetReceiptHandler(InboundDbContext db, IDistributedCache cache)
    : IRequestHandler<GetReceiptQuery, ReceiptDto?>
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    public async Task<ReceiptDto?> Handle(GetReceiptQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"inbound:receipts:{request.Id}";
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<ReceiptDto>(cached);
        }

        var receipt = await db.Receipts
            .AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.Id == request.Id)
            .Select(r => new ReceiptDto(
                r.Id,
                r.PurchaseOrderId,
                r.ReceivedAt,
                r.Notes,
                r.Lines
                    .OrderBy(l => l.CreatedAt)
                    .Select(l => new ReceiptLineDto(
                        l.Id,
                        l.PurchaseOrderLineId,
                        l.ProductId,
                        l.QuantityReceived.Value,
                        l.UnitCost.Amount,
                        l.UnitCost.Currency))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (receipt is not null)
        {
            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(receipt),
                CacheOptions,
                cancellationToken);
        }

        return receipt;
    }
}

// === Create Receipt ===
public sealed record ReceiptLineRequest(Guid PurchaseOrderLineId, decimal QuantityReceived);

public sealed record CreateReceiptCommand(
    Guid PurchaseOrderId,
    DateTime ReceivedAt,
    string? Notes,
    IReadOnlyList<ReceiptLineRequest> Lines)
    : IRequest<CreateReceiptResult>;

public sealed record CreateReceiptResult(bool Success, Guid? Id = null, string? Error = null);

public sealed class CreateReceiptHandler(InboundDbContext db, IDistributedCache cache)
    : IRequestHandler<CreateReceiptCommand, CreateReceiptResult>
{
    public async Task<CreateReceiptResult> Handle(CreateReceiptCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
            return new CreateReceiptResult(false, Error: "At least one receipt line is required.");

        var purchaseOrder = await db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken);

        if (purchaseOrder is null)
            return new CreateReceiptResult(false, Error: "Purchase order not found.");

        if (purchaseOrder.Status == PurchaseOrderStatus.Cancelled)
            return new CreateReceiptResult(false, Error: "Cannot receive against a cancelled purchase order.");

        var lineLookup = purchaseOrder.Lines.ToDictionary(l => l.Id);
        var seen = new HashSet<Guid>();
        var receivedLines = new List<(Guid LineId, Quantity Quantity)>();
        var receiptLines = new List<ReceiptLine>();

        foreach (var lineRequest in request.Lines)
        {
            if (!seen.Add(lineRequest.PurchaseOrderLineId))
                return new CreateReceiptResult(false, Error: "Duplicate purchase order lines are not allowed.");

            if (!lineLookup.TryGetValue(lineRequest.PurchaseOrderLineId, out var line))
                return new CreateReceiptResult(false, Error: "Receipt line does not belong to purchase order.");

            var quantityResult = Quantity.Create(lineRequest.QuantityReceived);
            if (quantityResult.IsFailure)
                return new CreateReceiptResult(false, Error: quantityResult.Error.Message);

            var receiptLineResult = ReceiptLine.Create(
                line.Id,
                line.ProductId,
                quantityResult.Value,
                line.UnitCost);

            if (receiptLineResult.IsFailure)
                return new CreateReceiptResult(false, Error: receiptLineResult.Error.Message);

            receivedLines.Add((line.Id, quantityResult.Value));
            receiptLines.Add(receiptLineResult.Value);
        }

        var applyResult = purchaseOrder.ApplyReceipt(receivedLines);
        if (applyResult.IsFailure)
            return new CreateReceiptResult(false, Error: applyResult.Error.Message);

        var receiptResult = Receipt.Create(
            purchaseOrder.Id,
            request.ReceivedAt,
            request.Notes,
            receiptLines);

        if (receiptResult.IsFailure)
            return new CreateReceiptResult(false, Error: receiptResult.Error.Message);

        db.Receipts.Add(receiptResult.Value);
        await db.SaveChangesAsync(cancellationToken);

        await CacheInvalidation.InvalidatePurchaseOrderCaches(cache, cancellationToken);
        await cache.RemoveAsync($"inbound:purchase-orders:{purchaseOrder.Id}", cancellationToken);
        await cache.RemoveAsync($"inbound:receipts:list:all", cancellationToken);
        await cache.RemoveAsync($"inbound:receipts:list:{purchaseOrder.Id}", cancellationToken);

        return new CreateReceiptResult(true, receiptResult.Value.Id);
    }
}

// === Endpoints ===
public static class ReceiptEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        var receipts = group.MapGroup("/receipts").WithTags("Receipts");

        receipts.MapGet("/", async (Guid? purchaseOrderId, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListReceiptsQuery(purchaseOrderId));
            return Results.Ok(result);
        })
        .WithName("ListReceipts")
        .WithSummary("List receipts (optional purchase order filter)");

        receipts.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetReceiptQuery(id));
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { error = "Receipt not found." });
        })
        .WithName("GetReceipt")
        .WithSummary("Get a receipt by ID");

        receipts.MapPost("/", async (CreateReceiptCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.Success
                ? Results.Created($"/inbound/receipts/{result.Id}", new { id = result.Id })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CreateReceipt")
        .WithSummary("Create a receipt for a purchase order");
    }
}
