using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Modules.Inbound.Domain.Entities;

public sealed class PurchaseOrderLineTests
{
    [Test]
    public async Task Create_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = Quantity.Create(10).Value;
        var unitCost = Money.Create(12.50m).Value;

        // Act
        var result = PurchaseOrderLine.Create(orderId, productId, quantity, unitCost);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.PurchaseOrderId).IsEqualTo(orderId);
        await Assert.That(result.Value.ProductId).IsEqualTo(productId);
        await Assert.That(result.Value.ReceivedQuantity.Value).IsEqualTo(0m);
    }

    [Test]
    public async Task Create_WithMissingProduct_ReturnsFailure()
    {
        // Arrange
        var quantity = Quantity.Create(1).Value;
        var unitCost = Money.Create(5).Value;

        // Act
        var result = PurchaseOrderLine.Create(Guid.NewGuid(), Guid.Empty, quantity, unitCost);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("ProductId");
    }

    [Test]
    public async Task Receive_WithValidQuantity_UpdatesReceivedQuantity()
    {
        // Arrange
        var line = PurchaseOrderLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Quantity.Create(10).Value,
            Money.Create(2).Value).Value;

        // Act
        var receiveResult = line.Receive(Quantity.Create(4).Value);

        // Assert
        await Assert.That(receiveResult.IsSuccess).IsTrue();
        await Assert.That(line.ReceivedQuantity.Value).IsEqualTo(4m);
    }

    [Test]
    public async Task Receive_OverOrderedQuantity_ReturnsFailure()
    {
        // Arrange
        var line = PurchaseOrderLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Quantity.Create(3).Value,
            Money.Create(2).Value).Value;

        // Act
        var receiveResult = line.Receive(Quantity.Create(5).Value);

        // Assert
        await Assert.That(receiveResult.IsFailure).IsTrue();
        await Assert.That(receiveResult.Error.Message).Contains("more than ordered");
    }

    [Test]
    public async Task IsFullyReceived_WhenPartiallyReceived_ReturnsFalse()
    {
        var line = PurchaseOrderLine.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            Quantity.Create(10).Value, Money.Create(2).Value).Value;
        line.Receive(Quantity.Create(4).Value);

        await Assert.That(line.IsFullyReceived).IsFalse();
    }

    [Test]
    public async Task IsFullyReceived_WhenFullyReceived_ReturnsTrue()
    {
        var line = PurchaseOrderLine.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            Quantity.Create(5).Value, Money.Create(2).Value).Value;
        line.Receive(Quantity.Create(5).Value);

        await Assert.That(line.IsFullyReceived).IsTrue();
    }

    [Test]
    public async Task MultiplePartialReceives_AccumulateCorrectly()
    {
        var line = PurchaseOrderLine.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            Quantity.Create(10).Value, Money.Create(2).Value).Value;

        line.Receive(Quantity.Create(3).Value);
        line.Receive(Quantity.Create(4).Value);

        await Assert.That(line.ReceivedQuantity.Value).IsEqualTo(7m);
        await Assert.That(line.IsFullyReceived).IsFalse();
    }

    [Test]
    public async Task Receive_ZeroQuantity_Fails()
    {
        var line = PurchaseOrderLine.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            Quantity.Create(5).Value, Money.Create(2).Value).Value;

        var result = line.Receive(Quantity.Zero);
        await Assert.That(result.IsFailure).IsTrue();
    }
}
