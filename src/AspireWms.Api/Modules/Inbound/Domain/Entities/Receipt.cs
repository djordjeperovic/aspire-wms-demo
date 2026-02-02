using AspireWms.Api.Shared.Domain;

namespace AspireWms.Api.Modules.Inbound.Domain.Entities;

public sealed class Receipt : Entity<Guid>
{
    public Guid PurchaseOrderId { get; private set; }
    public PurchaseOrder PurchaseOrder { get; private set; } = default!;
    public DateTime ReceivedAt { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<ReceiptLine> _lines = [];
    public IReadOnlyCollection<ReceiptLine> Lines => _lines.AsReadOnly();

    private Receipt() { }

    private Receipt(Guid id, Guid purchaseOrderId, DateTime receivedAt, string? notes)
        : base(id)
    {
        PurchaseOrderId = purchaseOrderId;
        ReceivedAt = receivedAt;
        Notes = notes;
    }

    public static Result<Receipt> Create(
        Guid purchaseOrderId,
        DateTime receivedAt,
        string? notes,
        IEnumerable<ReceiptLine> lines)
    {
        if (purchaseOrderId == Guid.Empty)
            return Error.Validation("Receipt.PurchaseOrderId", "PurchaseOrderId is required.");

        if (receivedAt == default)
            return Error.Validation("Receipt.ReceivedAt", "ReceivedAt is required.");

        var lineList = lines.ToList();
        if (lineList.Count == 0)
            return Error.Validation("Receipt.Lines", "Receipt must contain at least one line.");

        var receipt = new Receipt(Guid.NewGuid(), purchaseOrderId, receivedAt, notes?.Trim());
        receipt._lines.AddRange(lineList);

        return receipt;
    }
}
