using AspireWms.Api.Shared.Domain;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.Api.Modules.Inbound.Domain.Entities;

public sealed class ReceiptLine : Entity<Guid>
{
    public Guid ReceiptId { get; private set; }
    public Receipt Receipt { get; private set; } = default!;
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Quantity QuantityReceived { get; private set; } = default!;
    public Money UnitCost { get; private set; } = default!;

    private ReceiptLine() { }

    private ReceiptLine(Guid id, Guid purchaseOrderLineId, Guid productId, Quantity quantityReceived, Money unitCost)
        : base(id)
    {
        PurchaseOrderLineId = purchaseOrderLineId;
        ProductId = productId;
        QuantityReceived = quantityReceived;
        UnitCost = unitCost;
    }

    public static Result<ReceiptLine> Create(
        Guid purchaseOrderLineId,
        Guid productId,
        Quantity quantityReceived,
        Money unitCost)
    {
        if (purchaseOrderLineId == Guid.Empty)
            return Error.Validation("ReceiptLine.PurchaseOrderLineId", "PurchaseOrderLineId is required.");

        if (productId == Guid.Empty)
            return Error.Validation("ReceiptLine.ProductId", "ProductId is required.");

        if (quantityReceived.IsZero)
            return Error.Validation("ReceiptLine.QuantityReceived", "Received quantity must be greater than zero.");

        return new ReceiptLine(Guid.NewGuid(), purchaseOrderLineId, productId, quantityReceived, unitCost);
    }
}
