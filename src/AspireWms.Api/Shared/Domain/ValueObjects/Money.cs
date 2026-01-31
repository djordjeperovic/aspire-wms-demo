namespace AspireWms.Api.Shared.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            return Error.Validation("Money.Negative", "Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            return Error.Validation("Money.InvalidCurrency", "Currency cannot be empty.");

        if (currency.Length != 3)
            return Error.Validation("Money.InvalidCurrency", "Currency must be a 3-letter ISO code.");

        return new Money(Math.Round(amount, 2), currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = "USD") => new(0, currency.ToUpperInvariant());

    public static Result<Money> operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            return Error.Validation("Money.CurrencyMismatch", 
                $"Cannot add {left.Currency} and {right.Currency}.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Result<Money> operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            return Error.Validation("Money.CurrencyMismatch", 
                $"Cannot subtract {left.Currency} and {right.Currency}.");

        var result = left.Amount - right.Amount;
        if (result < 0)
            return Error.Validation("Money.Negative", "Result cannot be negative.");

        return new Money(result, left.Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative.", nameof(factor));

        return new Money(Math.Round(Amount * factor, 2), Currency);
    }

    public bool IsZero => Amount == 0;

    public override string ToString() => $"{Amount:N2} {Currency}";
}
