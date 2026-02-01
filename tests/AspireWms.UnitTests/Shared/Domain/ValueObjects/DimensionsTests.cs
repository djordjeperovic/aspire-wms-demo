using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Shared.Domain.ValueObjects;

public sealed class DimensionsTests
{
    [Test]
    public async Task Create_WithValidDimensions_ReturnsSuccess()
    {
        // Act
        var result = Dimensions.Create(10m, 5m, 2m);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Length).IsEqualTo(10m);
        await Assert.That(result.Value.Width).IsEqualTo(5m);
        await Assert.That(result.Value.Height).IsEqualTo(2m);
    }

    [Test]
    public async Task Create_WithZeroDimensions_ReturnsSuccess()
    {
        // Act
        var result = Dimensions.Create(0m, 0m, 0m);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.IsZero).IsTrue();
    }

    [Test]
    public async Task Create_WithNegativeLength_ReturnsFailure()
    {
        // Act
        var result = Dimensions.Create(-1m, 5m, 2m);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Dimensions.Length");
        await Assert.That(result.Error.Message).Contains("Length cannot be negative");
    }

    [Test]
    public async Task Create_WithNegativeWidth_ReturnsFailure()
    {
        // Act
        var result = Dimensions.Create(10m, -5m, 2m);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Dimensions.Width");
        await Assert.That(result.Error.Message).Contains("Width cannot be negative");
    }

    [Test]
    public async Task Create_WithNegativeHeight_ReturnsFailure()
    {
        // Act
        var result = Dimensions.Create(10m, 5m, -2m);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Dimensions.Height");
        await Assert.That(result.Error.Message).Contains("Height cannot be negative");
    }

    [Test]
    public async Task Volume_CalculatesCorrectly()
    {
        // Arrange
        var dimensions = Dimensions.Create(10m, 5m, 2m).Value;

        // Act
        var volume = dimensions.Volume;

        // Assert
        await Assert.That(volume).IsEqualTo(100m);
    }

    [Test]
    public async Task Volume_WithZeroDimension_ReturnsZero()
    {
        // Arrange
        var dimensions = Dimensions.Create(10m, 0m, 2m).Value;

        // Act
        var volume = dimensions.Volume;

        // Assert
        await Assert.That(volume).IsEqualTo(0m);
    }

    [Test]
    public async Task IsZero_WithAnyZeroDimension_ReturnsTrue()
    {
        // Arrange
        var dimensions1 = Dimensions.Create(0m, 5m, 2m).Value;
        var dimensions2 = Dimensions.Create(10m, 0m, 2m).Value;
        var dimensions3 = Dimensions.Create(10m, 5m, 0m).Value;

        // Assert
        await Assert.That(dimensions1.IsZero).IsTrue();
        await Assert.That(dimensions2.IsZero).IsTrue();
        await Assert.That(dimensions3.IsZero).IsTrue();
    }

    [Test]
    public async Task IsZero_WithAllNonZero_ReturnsFalse()
    {
        // Arrange
        var dimensions = Dimensions.Create(10m, 5m, 2m).Value;

        // Assert
        await Assert.That(dimensions.IsZero).IsFalse();
    }

    [Test]
    public async Task ToString_FormatsCorrectly()
    {
        // Arrange
        var dimensions = Dimensions.Create(10.5m, 5.25m, 2.75m).Value;

        // Act
        var text = dimensions.ToString();

        // Assert
        await Assert.That(text).IsEqualTo("10.50 × 5.25 × 2.75");
    }

    [Test]
    public async Task Equality_SameValues_AreEqual()
    {
        // Arrange
        var dimensions1 = Dimensions.Create(10m, 5m, 2m).Value;
        var dimensions2 = Dimensions.Create(10m, 5m, 2m).Value;

        // Assert
        await Assert.That(dimensions1 == dimensions2).IsTrue();
        await Assert.That(dimensions1.Equals(dimensions2)).IsTrue();
    }

    [Test]
    public async Task Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var dimensions1 = Dimensions.Create(10m, 5m, 2m).Value;
        var dimensions2 = Dimensions.Create(10m, 5m, 3m).Value;

        // Assert
        await Assert.That(dimensions1 == dimensions2).IsFalse();
        await Assert.That(dimensions1.Equals(dimensions2)).IsFalse();
    }
}
