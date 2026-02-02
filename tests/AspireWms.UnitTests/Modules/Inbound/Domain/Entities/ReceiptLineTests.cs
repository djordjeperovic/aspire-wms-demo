using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Modules.Inbound.Domain.Entities;

public sealed class ReceiptLineTests
{
    [Test]
    public async Task Create_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var poLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = Quantity.Create(5).Value;
        var unitCost = Money.Create(10m).Value;

        // Act
        var result = ReceiptLine.Create(poLineId, productId, quantity, unitCost);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.PurchaseOrderLineId).IsEqualTo(poLineId);
        await Assert.That(result.Value.ProductId).IsEqualTo(productId);
        await Assert.That(result.Value.QuantityReceived.Value).IsEqualTo(5m);
    }

    [Test]
    public async Task Create_WithEmptyPurchaseOrderLineId_ReturnsFailure()
    {
        // Act
        var result = ReceiptLine.Create(
            Guid.Empty,
            Guid.NewGuid(),
            Quantity.Create(1).Value,
            Money.Create(5m).Value);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("PurchaseOrderLineId");
    }

    [Test]
    public async Task Create_WithEmptyProductId_ReturnsFailure()
    {
        // Act
        var result = ReceiptLine.Create(
            Guid.NewGuid(),
            Guid.Empty,
            Quantity.Create(1).Value,
            Money.Create(5m).Value);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("ProductId");
    }

    [Test]
    public async Task Create_WithZeroQuantity_ReturnsFailure()
    {
        // Act
        var result = ReceiptLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Quantity.Zero,
            Money.Create(5m).Value);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("greater than zero");
    }
}
