using AspireWms.Api.Modules.Inventory.Domain.Enums;
using AspireWms.Api.Shared.Domain;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.Api.Modules.Inventory.Domain.Entities;

/// <summary>
/// Audit record of stock changes for an inventory item.
/// </summary>
public sealed class StockMovement : Entity<Guid>
{
    public Guid InventoryItemId { get; private set; }
    public InventoryItem InventoryItem { get; private set; } = null!;

    public MovementType MovementType { get; private set; }
    public Quantity Quantity { get; private set; } = null!;
    public string Reason { get; private set; } = string.Empty;

    private StockMovement() : base() { }

    private StockMovement(Guid id, Guid inventoryItemId, MovementType movementType, Quantity quantity, string reason)
        : base(id)
    {
        InventoryItemId = inventoryItemId;
        MovementType = movementType;
        Quantity = quantity;
        Reason = reason;
    }

    public static Result<StockMovement> Create(
        Guid inventoryItemId,
        MovementType movementType,
        Quantity quantity,
        string reason)
    {
        if (inventoryItemId == Guid.Empty)
            return Error.Validation("StockMovement.InventoryItemId", "Inventory item ID is required.");

        if (string.IsNullOrWhiteSpace(reason))
            return Error.Validation("StockMovement.Reason", "Reason is required for stock movements.");

        if (reason.Length > 500)
            return Error.Validation("StockMovement.Reason", "Reason cannot exceed 500 characters.");

        return new StockMovement(
            Guid.NewGuid(),
            inventoryItemId,
            movementType,
            quantity,
            reason.Trim());
    }

    /// <summary>
    /// Indicates if this movement increases stock.
    /// </summary>
    public bool IsInbound => MovementType switch
    {
        MovementType.Initial => true,
        MovementType.Received => true,
        MovementType.AdjustmentIn => true,
        MovementType.Return => true,
        _ => false
    };

    /// <summary>
    /// Indicates if this movement decreases stock.
    /// </summary>
    public bool IsOutbound => !IsInbound;
}
