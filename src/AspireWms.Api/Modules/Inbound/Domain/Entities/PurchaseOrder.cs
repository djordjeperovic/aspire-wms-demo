using AspireWms.Api.Shared.Domain;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.Api.Modules.Inbound.Domain.Entities;

public sealed class PurchaseOrder : Entity<Guid>
{
    public string OrderNumber { get; private set; } = string.Empty;
    public string SupplierName { get; private set; } = string.Empty;
    public PurchaseOrderStatus Status { get; private set; }
    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = [];
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    private PurchaseOrder() { }

    private PurchaseOrder(
        Guid id,
        string orderNumber,
        string supplierName,
        DateOnly? expectedDeliveryDate,
        string? notes)
        : base(id)
    {
        OrderNumber = orderNumber;
        SupplierName = supplierName;
        ExpectedDeliveryDate = expectedDeliveryDate;
        Notes = notes;
        Status = PurchaseOrderStatus.Draft;
    }

    public static Result<PurchaseOrder> Create(
        string orderNumber,
        string supplierName,
        DateOnly? expectedDeliveryDate = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return Error.Validation("PurchaseOrder.OrderNumber", "Order number is required.");

        if (orderNumber.Length > 50)
            return Error.Validation("PurchaseOrder.OrderNumber", "Order number cannot exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(supplierName))
            return Error.Validation("PurchaseOrder.SupplierName", "Supplier name is required.");

        if (supplierName.Length > 200)
            return Error.Validation("PurchaseOrder.SupplierName", "Supplier name cannot exceed 200 characters.");

        return new PurchaseOrder(
            Guid.NewGuid(),
            orderNumber.Trim().ToUpperInvariant(),
            supplierName.Trim(),
            expectedDeliveryDate,
            notes?.Trim());
    }

    public Result<PurchaseOrderLine> AddLine(Guid productId, Quantity quantity, Money unitCost)
    {
        if (Status == PurchaseOrderStatus.Cancelled)
            return Error.Validation("PurchaseOrder.Status", "Cannot add lines to a cancelled purchase order.");

        if (_lines.Any(l => l.ProductId == productId))
            return Error.Conflict("PurchaseOrderLine.ProductId", "Product already exists on this purchase order.");

        var lineResult = PurchaseOrderLine.Create(Id, productId, quantity, unitCost);
        if (lineResult.IsFailure)
            return lineResult.Error;

        _lines.Add(lineResult.Value);
        MarkUpdated();

        return lineResult.Value;
    }

    public Result Submit()
    {
        if (Status != PurchaseOrderStatus.Draft)
            return Error.Validation("PurchaseOrder.Status", "Only draft purchase orders can be submitted.");

        Status = PurchaseOrderStatus.Submitted;
        MarkUpdated();

        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == PurchaseOrderStatus.FullyReceived)
            return Error.Validation("PurchaseOrder.Status", "Cannot cancel a fully received purchase order.");

        if (Status == PurchaseOrderStatus.Cancelled)
            return Error.Validation("PurchaseOrder.Status", "Purchase order is already cancelled.");

        Status = PurchaseOrderStatus.Cancelled;
        MarkUpdated();

        return Result.Success();
    }

    public Result ApplyReceipt(IReadOnlyCollection<(Guid LineId, Quantity Quantity)> receivedLines)
    {
        if (Status == PurchaseOrderStatus.Cancelled)
            return Error.Validation("PurchaseOrder.Status", "Cannot receive against a cancelled purchase order.");

        if (Status == PurchaseOrderStatus.FullyReceived)
            return Error.Validation("PurchaseOrder.Status", "Purchase order is already fully received.");

        if (receivedLines.Count == 0)
            return Error.Validation("PurchaseOrder.ReceiptLines", "At least one receipt line is required.");

        var seen = new HashSet<Guid>();
        foreach (var (lineId, quantity) in receivedLines)
        {
            if (!seen.Add(lineId))
                return Error.Validation("PurchaseOrder.ReceiptLines", "Duplicate purchase order lines are not allowed.");

            var line = _lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null)
                return Error.NotFound("PurchaseOrderLine.Id", "Purchase order line not found.");

            var receiveResult = line.Receive(quantity);
            if (receiveResult.IsFailure)
                return receiveResult.Error;
        }

        Status = _lines.All(l => l.IsFullyReceived)
            ? PurchaseOrderStatus.FullyReceived
            : PurchaseOrderStatus.PartiallyReceived;

        MarkUpdated();
        return Result.Success();
    }
}
