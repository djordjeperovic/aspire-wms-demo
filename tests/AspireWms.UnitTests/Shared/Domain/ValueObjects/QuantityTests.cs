using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Shared.Domain.ValueObjects;

public class QuantityTests
{
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(100.5)]
    [Arguments(999999.99)]
    public async Task Create_WithValidValue_ShouldSucceed(decimal value)
    {
        // Act
        var result = Quantity.Create(value);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo(value);
    }

    [Test]
    [Arguments(-1)]
    [Arguments(-0.01)]
    [Arguments(-1000)]
    public async Task Create_WithNegativeValue_ShouldFail(decimal value)
    {
        // Act
        var result = Quantity.Create(value);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Quantity.Negative");
    }

    [Test]
    public async Task Zero_ShouldReturnZeroQuantity()
    {
        // Act
        var zero = Quantity.Zero;

        // Assert
        await Assert.That(zero.Value).IsEqualTo(0);
        await Assert.That(zero.IsZero).IsTrue();
    }

    [Test]
    public async Task Addition_ShouldAddQuantities()
    {
        // Arrange
        var q1 = Quantity.Create(10).Value;
        var q2 = Quantity.Create(5).Value;

        // Act
        var result = q1 + q2;

        // Assert
        await Assert.That(result.Value).IsEqualTo(15);
    }

    [Test]
    public async Task Subtraction_WithSufficientStock_ShouldSucceed()
    {
        // Arrange
        var q1 = Quantity.Create(10).Value;
        var q2 = Quantity.Create(3).Value;

        // Act
        var result = q1 - q2;

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo(7);
    }

    [Test]
    public async Task Subtraction_WithInsufficientStock_ShouldFail()
    {
        // Arrange
        var q1 = Quantity.Create(5).Value;
        var q2 = Quantity.Create(10).Value;

        // Act
        var result = q1 - q2;

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Quantity.InsufficientStock");
    }

    [Test]
    public async Task Comparison_ShouldWorkCorrectly()
    {
        // Arrange
        var small = Quantity.Create(5).Value;
        var large = Quantity.Create(10).Value;

        // Assert
        await Assert.That(small < large).IsTrue();
        await Assert.That(large > small).IsTrue();
        await Assert.That(small <= large).IsTrue();
        await Assert.That(large >= small).IsTrue();
    }

    [Test]
    public async Task ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var quantity = Quantity.Create(123.456m).Value;

        // Act
        var str = quantity.ToString();

        // Assert
        await Assert.That(str).IsEqualTo("123.46");
    }
}
