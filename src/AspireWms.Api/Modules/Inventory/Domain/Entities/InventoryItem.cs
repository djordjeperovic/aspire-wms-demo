using AspireWms.Api.Modules.Inventory.Domain.Enums;
using AspireWms.Api.Shared.Domain;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.Api.Modules.Inventory.Domain.Entities;

/// <summary>
/// Represents stock of a product at a specific location.
/// </summary>
public sealed class InventoryItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public Guid LocationId { get; private set; }
    public Location Location { get; private set; } = null!;

    public Quantity Quantity { get; private set; } = null!;

    private readonly List<StockMovement> _movements = [];
    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();

    private InventoryItem() : base() { }

    private InventoryItem(Guid id, Guid productId, Guid locationId, Quantity quantity)
        : base(id)
    {
        ProductId = productId;
        LocationId = locationId;
        Quantity = quantity;
    }

    public static Result<InventoryItem> Create(Guid productId, Guid locationId, Quantity initialQuantity, string? reason = null)
    {
        if (productId == Guid.Empty)
            return Error.Validation("InventoryItem.ProductId", "Product ID is required.");

        if (locationId == Guid.Empty)
            return Error.Validation("InventoryItem.LocationId", "Location ID is required.");

        var item = new InventoryItem(
            Guid.NewGuid(),
            productId,
            locationId,
            initialQuantity);

        // Record initial movement
        var movement = StockMovement.Create(
            item.Id,
            MovementType.Initial,
            initialQuantity,
            reason ?? "Initial stock");

        if (movement.IsFailure)
            return movement.Error;

        item._movements.Add(movement.Value);

        return item;
    }

    public Result<StockMovement> AddStock(Quantity quantity, MovementType movementType, string reason)
    {
        var movementResult = StockMovement.Create(Id, movementType, quantity, reason);
        if (movementResult.IsFailure)
            return movementResult.Error;

        Quantity += quantity;
        _movements.Add(movementResult.Value);
        MarkUpdated();

        return movementResult.Value;
    }

    public Result<StockMovement> RemoveStock(Quantity quantity, MovementType movementType, string reason)
    {
        var subtractResult = Quantity - quantity;
        if (subtractResult.IsFailure)
            return Error.Validation("InventoryItem.Quantity", $"Insufficient stock. Available: {Quantity.Value}, Requested: {quantity.Value}");

        var negativeQuantityResult = Quantity.Create(-quantity.Value);
        if (negativeQuantityResult.IsFailure)
        {
            // Create movement with the quantity that was removed (recorded as quantity, movement type indicates direction)
            var movementResult = StockMovement.Create(Id, movementType, quantity, reason);
            if (movementResult.IsFailure)
                return movementResult.Error;

            Quantity = subtractResult.Value;
            _movements.Add(movementResult.Value);
            MarkUpdated();

            return movementResult.Value;
        }

        return Error.Validation("InventoryItem.Quantity", "Invalid quantity operation.");
    }

    public Result<StockMovement> AdjustStock(int adjustment, string reason)
    {
        if (adjustment == 0)
            return Error.Validation("InventoryItem.Adjustment", "Adjustment cannot be zero.");

        var quantityResult = Quantity.Create(Math.Abs(adjustment));
        if (quantityResult.IsFailure)
            return quantityResult.Error;

        return adjustment > 0
            ? AddStock(quantityResult.Value, MovementType.AdjustmentIn, reason)
            : RemoveStock(quantityResult.Value, MovementType.AdjustmentOut, reason);
    }
}
