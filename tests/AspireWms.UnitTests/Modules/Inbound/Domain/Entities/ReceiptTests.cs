using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Modules.Inbound.Domain.Entities;

public sealed class ReceiptTests
{
    [Test]
    public async Task Create_WithNoLines_ReturnsFailure()
    {
        // Act
        var result = Receipt.Create(Guid.NewGuid(), DateTime.UtcNow, "Notes", []);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("at least one line");
    }

    [Test]
    public async Task Create_WithValidLines_ReturnsSuccess()
    {
        // Arrange
        var receiptLine = ReceiptLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Quantity.Create(2).Value,
            Money.Create(1.25m).Value).Value;

        // Act
        var result = Receipt.Create(Guid.NewGuid(), DateTime.UtcNow, "Delivery", [receiptLine]);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Lines.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Create_WithEmptyPurchaseOrderId_ReturnsFailure()
    {
        var receiptLine = ReceiptLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Quantity.Create(1).Value,
            Money.Create(1m).Value).Value;

        var result = Receipt.Create(Guid.Empty, DateTime.UtcNow, null, [receiptLine]);

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("PurchaseOrderId");
    }
}
