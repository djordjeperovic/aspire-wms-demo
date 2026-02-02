using AspireWms.Api.Shared.Domain;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.Api.Modules.Inbound.Domain.Entities;

public sealed class PurchaseOrderLine : Entity<Guid>
{
    public Guid PurchaseOrderId { get; private set; }
    public PurchaseOrder PurchaseOrder { get; private set; } = default!;
    public Guid ProductId { get; private set; }
    public Quantity Quantity { get; private set; } = default!;
    public Money UnitCost { get; private set; } = default!;
    public Quantity ReceivedQuantity { get; private set; } = Quantity.Zero;

    private PurchaseOrderLine() { }

    private PurchaseOrderLine(Guid id, Guid purchaseOrderId, Guid productId, Quantity quantity, Money unitCost)
        : base(id)
    {
        PurchaseOrderId = purchaseOrderId;
        ProductId = productId;
        Quantity = quantity;
        UnitCost = unitCost;
        ReceivedQuantity = Quantity.Zero;
    }

    public static Result<PurchaseOrderLine> Create(Guid purchaseOrderId, Guid productId, Quantity quantity, Money unitCost)
    {
        if (purchaseOrderId == Guid.Empty)
            return Error.Validation("PurchaseOrderLine.PurchaseOrderId", "PurchaseOrderId is required.");

        if (productId == Guid.Empty)
            return Error.Validation("PurchaseOrderLine.ProductId", "ProductId is required.");

        if (quantity.IsZero)
            return Error.Validation("PurchaseOrderLine.Quantity", "Quantity must be greater than zero.");

        return new PurchaseOrderLine(Guid.NewGuid(), purchaseOrderId, productId, quantity, unitCost);
    }

    public Result Receive(Quantity quantityReceived)
    {
        if (quantityReceived.IsZero)
            return Error.Validation("PurchaseOrderLine.QuantityReceived", "Received quantity must be greater than zero.");

        var updated = ReceivedQuantity + quantityReceived;
        if (updated > Quantity)
            return Error.Validation("PurchaseOrderLine.OverReceive", "Cannot receive more than ordered.");

        ReceivedQuantity = updated;
        MarkUpdated();

        return Result.Success();
    }

    public bool IsFullyReceived => ReceivedQuantity >= Quantity;
}
