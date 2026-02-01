using AspireWms.Api.Modules.Inventory.Domain.Entities;

namespace AspireWms.UnitTests.Modules.Inventory.Domain.Entities;

public sealed class ProductTests
{
    [Test]
    public async Task Create_WithValidData_ReturnsSuccessWithProduct()
    {
        // Act
        var result = Product.Create("SKU001", "Test Product", "Description", 1.5m, 10m, 5m, 2m);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Sku).IsEqualTo("SKU001");
        await Assert.That(result.Value.Name).IsEqualTo("Test Product");
        await Assert.That(result.Value.Weight).IsEqualTo(1.5m);
        await Assert.That(result.Value.IsActive).IsTrue();
    }

    [Test]
    public async Task Create_NormalizesSku_ToUpperCase()
    {
        // Act
        var result = Product.Create("sku-test", "Test Product");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Sku).IsEqualTo("SKU-TEST");
    }

    [Test]
    [Arguments("", "SKU is required")]
    [Arguments("   ", "SKU is required")]
    public async Task Create_WithInvalidSku_ReturnsFailure(string sku, string expectedError)
    {
        // Act
        var result = Product.Create(sku, "Test Product");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains(expectedError);
    }

    [Test]
    public async Task Create_WithSkuTooLong_ReturnsFailure()
    {
        // Arrange
        var longSku = new string('X', 51);

        // Act
        var result = Product.Create(longSku, "Test Product");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("50 characters");
    }

    [Test]
    [Arguments("", "Name is required")]
    [Arguments("   ", "Name is required")]
    public async Task Create_WithInvalidName_ReturnsFailure(string name, string expectedError)
    {
        // Act
        var result = Product.Create("SKU001", name);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains(expectedError);
    }

    [Test]
    public async Task Create_WithNegativeWeight_ReturnsFailure()
    {
        // Act
        var result = Product.Create("SKU001", "Test Product", weight: -1);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Weight cannot be negative");
    }

    [Test]
    [Arguments(-1, 0, 0)]
    [Arguments(0, -1, 0)]
    [Arguments(0, 0, -1)]
    public async Task Create_WithNegativeDimensions_ReturnsFailure(decimal l, decimal w, decimal h)
    {
        // Act
        var result = Product.Create("SKU001", "Test Product", length: l, width: w, height: h);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("cannot be negative");
    }

    [Test]
    public async Task Volume_CalculatesCorrectly()
    {
        // Arrange
        var result = Product.Create("SKU001", "Test Product", length: 10, width: 5, height: 2);

        // Assert
        await Assert.That(result.Value.Volume).IsEqualTo(100m);
    }

    [Test]
    public async Task Update_WithValidData_UpdatesProduct()
    {
        // Arrange
        var product = Product.Create("SKU001", "Test Product").Value;

        // Act
        var updateResult = product.Update("Updated Name", "New Description", 2.0m, 20m, 10m, 5m);

        // Assert
        await Assert.That(updateResult.IsSuccess).IsTrue();
        await Assert.That(product.Name).IsEqualTo("Updated Name");
        await Assert.That(product.Description).IsEqualTo("New Description");
        await Assert.That(product.Weight).IsEqualTo(2.0m);
        await Assert.That(product.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var product = Product.Create("SKU001", "Test Product").Value;

        // Act
        product.Deactivate();

        // Assert
        await Assert.That(product.IsActive).IsFalse();
        await Assert.That(product.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var product = Product.Create("SKU001", "Test Product").Value;
        product.Deactivate();

        // Act
        product.Activate();

        // Assert
        await Assert.That(product.IsActive).IsTrue();
    }

    [Test]
    public async Task Equality_SameId_AreEqual()
    {
        // Arrange
        var product1 = Product.Create("SKU001", "Product 1").Value;
        var product2 = product1; // Same reference

        // Assert
        await Assert.That(product1 == product2).IsTrue();
        await Assert.That(product1.Equals(product2)).IsTrue();
    }

    [Test]
    public async Task Equality_DifferentId_AreNotEqual()
    {
        // Arrange
        var product1 = Product.Create("SKU001", "Product 1").Value;
        var product2 = Product.Create("SKU002", "Product 2").Value;

        // Assert
        await Assert.That(product1 != product2).IsTrue();
        await Assert.That(product1.Equals(product2)).IsFalse();
    }
}
