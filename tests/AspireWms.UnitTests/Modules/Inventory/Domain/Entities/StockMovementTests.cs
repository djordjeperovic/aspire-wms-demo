using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Domain.Enums;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Modules.Inventory.Domain.Entities;

public sealed class StockMovementTests
{
    [Test]
    public async Task Create_WithValidData_ReturnsSuccessWithMovement()
    {
        // Arrange
        var inventoryItemId = Guid.NewGuid();
        var quantity = Quantity.Create(10).Value;

        // Act
        var result = StockMovement.Create(inventoryItemId, MovementType.Received, quantity, "Test shipment");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.InventoryItemId).IsEqualTo(inventoryItemId);
        await Assert.That(result.Value.MovementType).IsEqualTo(MovementType.Received);
        await Assert.That(result.Value.Quantity.Value).IsEqualTo(10);
        await Assert.That(result.Value.Reason).IsEqualTo("Test shipment");
    }

    [Test]
    public async Task Create_WithEmptyInventoryItemId_ReturnsFailure()
    {
        // Arrange
        var quantity = Quantity.Create(10).Value;

        // Act
        var result = StockMovement.Create(Guid.Empty, MovementType.Received, quantity, "Test");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Inventory item ID is required");
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Create_WithInvalidReason_ReturnsFailure(string reason)
    {
        // Arrange
        var quantity = Quantity.Create(10).Value;

        // Act
        var result = StockMovement.Create(Guid.NewGuid(), MovementType.Received, quantity, reason);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Reason is required");
    }

    [Test]
    public async Task Create_WithReasonTooLong_ReturnsFailure()
    {
        // Arrange
        var quantity = Quantity.Create(10).Value;
        var longReason = new string('X', 501);

        // Act
        var result = StockMovement.Create(Guid.NewGuid(), MovementType.Received, quantity, longReason);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("500 characters");
    }

    [Test]
    [Arguments(MovementType.Initial, true)]
    [Arguments(MovementType.Received, true)]
    [Arguments(MovementType.AdjustmentIn, true)]
    [Arguments(MovementType.Return, true)]
    [Arguments(MovementType.Picked, false)]
    [Arguments(MovementType.AdjustmentOut, false)]
    [Arguments(MovementType.Damaged, false)]
    [Arguments(MovementType.Transfer, false)]
    public async Task IsInbound_ReturnsCorrectValue(MovementType type, bool expectedIsInbound)
    {
        // Arrange
        var quantity = Quantity.Create(10).Value;
        var movement = StockMovement.Create(Guid.NewGuid(), type, quantity, "Test").Value;

        // Assert
        await Assert.That(movement.IsInbound).IsEqualTo(expectedIsInbound);
        await Assert.That(movement.IsOutbound).IsEqualTo(!expectedIsInbound);
    }

    [Test]
    public async Task Create_TrimsReason()
    {
        // Arrange
        var quantity = Quantity.Create(10).Value;

        // Act
        var result = StockMovement.Create(Guid.NewGuid(), MovementType.Received, quantity, "  Test reason  ");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Reason).IsEqualTo("Test reason");
    }
}
