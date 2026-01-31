using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Shared.Domain.ValueObjects;

public class MoneyTests
{
    [Test]
    [Arguments(0, "USD")]
    [Arguments(100, "USD")]
    [Arguments(99.99, "EUR")]
    [Arguments(1000000, "GBP")]
    public async Task Create_WithValidValues_ShouldSucceed(decimal amount, string currency)
    {
        // Act
        var result = Money.Create(amount, currency);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Amount).IsEqualTo(amount);
        await Assert.That(result.Value.Currency).IsEqualTo(currency.ToUpperInvariant());
    }

    [Test]
    public async Task Create_WithNegativeAmount_ShouldFail()
    {
        // Act
        var result = Money.Create(-10, "USD");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Money.Negative");
    }

    [Test]
    [Arguments("")]
    [Arguments("US")]
    [Arguments("USDD")]
    public async Task Create_WithInvalidCurrency_ShouldFail(string currency)
    {
        // Act
        var result = Money.Create(100, currency);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Money.InvalidCurrency");
    }

    [Test]
    public async Task Create_ShouldRoundToTwoDecimals()
    {
        // Act
        var result = Money.Create(10.999m, "USD");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Amount).IsEqualTo(11.00m);
    }

    [Test]
    public async Task Create_ShouldNormalizeCurrencyToUppercase()
    {
        // Act
        var result = Money.Create(100, "usd");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Currency).IsEqualTo("USD");
    }

    [Test]
    public async Task Zero_ShouldReturnZeroMoney()
    {
        // Act
        var zero = Money.Zero("EUR");

        // Assert
        await Assert.That(zero.Amount).IsEqualTo(0);
        await Assert.That(zero.Currency).IsEqualTo("EUR");
        await Assert.That(zero.IsZero).IsTrue();
    }

    [Test]
    public async Task Addition_WithSameCurrency_ShouldSucceed()
    {
        // Arrange
        var m1 = Money.Create(10.50m, "USD").Value;
        var m2 = Money.Create(5.25m, "USD").Value;

        // Act
        var result = m1 + m2;

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Amount).IsEqualTo(15.75m);
        await Assert.That(result.Value.Currency).IsEqualTo("USD");
    }

    [Test]
    public async Task Addition_WithDifferentCurrencies_ShouldFail()
    {
        // Arrange
        var m1 = Money.Create(10, "USD").Value;
        var m2 = Money.Create(10, "EUR").Value;

        // Act
        var result = m1 + m2;

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Money.CurrencyMismatch");
    }

    [Test]
    public async Task Subtraction_WithSufficientFunds_ShouldSucceed()
    {
        // Arrange
        var m1 = Money.Create(100, "USD").Value;
        var m2 = Money.Create(30, "USD").Value;

        // Act
        var result = m1 - m2;

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Amount).IsEqualTo(70);
    }

    [Test]
    public async Task Subtraction_WithInsufficientFunds_ShouldFail()
    {
        // Arrange
        var m1 = Money.Create(10, "USD").Value;
        var m2 = Money.Create(20, "USD").Value;

        // Act
        var result = m1 - m2;

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Money.Negative");
    }

    [Test]
    public async Task Multiply_ShouldMultiplyAndRound()
    {
        // Arrange
        var money = Money.Create(10, "USD").Value;

        // Act
        var result = money.Multiply(3.333m);

        // Assert
        await Assert.That(result.Amount).IsEqualTo(33.33m);
        await Assert.That(result.Currency).IsEqualTo("USD");
    }

    [Test]
    public async Task ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = Money.Create(1234.56m, "EUR").Value;

        // Act
        var str = money.ToString();

        // Assert
        await Assert.That(str).IsEqualTo("1,234.56 EUR");
    }
}
