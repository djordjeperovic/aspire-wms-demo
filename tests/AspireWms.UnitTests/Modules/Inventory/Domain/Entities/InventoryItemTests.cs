using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Domain.Enums;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Modules.Inventory.Domain.Entities;

public sealed class InventoryItemTests
{
    [Test]
    public async Task Create_WithValidData_ReturnsSuccessWithItem()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var quantity = Quantity.Create(10).Value;

        // Act
        var result = InventoryItem.Create(productId, locationId, quantity, "Initial stock");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.ProductId).IsEqualTo(productId);
        await Assert.That(result.Value.LocationId).IsEqualTo(locationId);
        await Assert.That(result.Value.Quantity.Value).IsEqualTo(10);
    }

    [Test]
    public async Task Create_CreatesInitialMovement()
    {
        // Arrange
        var quantity = Quantity.Create(50).Value;

        // Act
        var result = InventoryItem.Create(Guid.NewGuid(), Guid.NewGuid(), quantity, "Test initial");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Movements.Count).IsEqualTo(1);
        
        var movement = result.Value.Movements.First();
        await Assert.That(movement.MovementType).IsEqualTo(MovementType.Initial);
        await Assert.That(movement.Quantity.Value).IsEqualTo(50);
        await Assert.That(movement.Reason).IsEqualTo("Test initial");
    }

    [Test]
    public async Task Create_WithEmptyProductId_ReturnsFailure()
    {
        // Arrange
        var quantity = Quantity.Create(10).Value;

        // Act
        var result = InventoryItem.Create(Guid.Empty, Guid.NewGuid(), quantity);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Product ID is required");
    }

    [Test]
    public async Task Create_WithEmptyLocationId_ReturnsFailure()
    {
        // Arrange
        var quantity = Quantity.Create(10).Value;

        // Act
        var result = InventoryItem.Create(Guid.NewGuid(), Guid.Empty, quantity);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Location ID is required");
    }

    [Test]
    public async Task AddStock_IncreasesQuantityAndCreatesMovement()
    {
        // Arrange
        var item = InventoryItem.Create(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Quantity.Create(10).Value, 
            "Initial").Value;
        var addQuantity = Quantity.Create(5).Value;

        // Act
        var result = item.AddStock(addQuantity, MovementType.Received, "Received shipment");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(item.Quantity.Value).IsEqualTo(15);
        await Assert.That(item.Movements.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RemoveStock_DecreasesQuantityAndCreatesMovement()
    {
        // Arrange
        var item = InventoryItem.Create(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Quantity.Create(10).Value, 
            "Initial").Value;
        var removeQuantity = Quantity.Create(3).Value;

        // Act
        var result = item.RemoveStock(removeQuantity, MovementType.Picked, "Order picked");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(item.Quantity.Value).IsEqualTo(7);
        await Assert.That(item.Movements.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RemoveStock_WhenInsufficientStock_ReturnsFailure()
    {
        // Arrange
        var item = InventoryItem.Create(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Quantity.Create(5).Value, 
            "Initial").Value;
        var removeQuantity = Quantity.Create(10).Value;

        // Act
        var result = item.RemoveStock(removeQuantity, MovementType.Picked, "Over-pick attempt");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Insufficient stock");
    }

    [Test]
    public async Task AdjustStock_PositiveAdjustment_IncreasesStock()
    {
        // Arrange
        var item = InventoryItem.Create(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Quantity.Create(10).Value, 
            "Initial").Value;

        // Act
        var result = item.AdjustStock(5, "Count correction +5");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(item.Quantity.Value).IsEqualTo(15);
        
        var movement = item.Movements.Last();
        await Assert.That(movement.MovementType).IsEqualTo(MovementType.AdjustmentIn);
    }

    [Test]
    public async Task AdjustStock_NegativeAdjustment_DecreasesStock()
    {
        // Arrange
        var item = InventoryItem.Create(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Quantity.Create(10).Value, 
            "Initial").Value;

        // Act
        var result = item.AdjustStock(-3, "Damaged items -3");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(item.Quantity.Value).IsEqualTo(7);
        
        var movement = item.Movements.Last();
        await Assert.That(movement.MovementType).IsEqualTo(MovementType.AdjustmentOut);
    }

    [Test]
    public async Task AdjustStock_ZeroAdjustment_ReturnsFailure()
    {
        // Arrange
        var item = InventoryItem.Create(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Quantity.Create(10).Value, 
            "Initial").Value;

        // Act
        var result = item.AdjustStock(0, "No change");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Adjustment cannot be zero");
    }
}
