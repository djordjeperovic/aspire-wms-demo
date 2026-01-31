namespace AspireWms.Api.Shared.Domain.ValueObjects;

/// <summary>
/// Represents a non-negative quantity in the warehouse.
/// </summary>
public sealed record Quantity
{
    public decimal Value { get; }

    private Quantity(decimal value) => Value = value;

    public static Result<Quantity> Create(decimal value)
    {
        if (value < 0)
            return Error.Validation("Quantity.Negative", "Quantity cannot be negative.");

        return new Quantity(value);
    }

    public static Quantity Zero => new(0);

    public static Quantity operator +(Quantity left, Quantity right) =>
        new(left.Value + right.Value);

    public static Result<Quantity> operator -(Quantity left, Quantity right)
    {
        var result = left.Value - right.Value;
        return result < 0 
            ? Error.Validation("Quantity.InsufficientStock", $"Cannot subtract {right.Value} from {left.Value}.")
            : new Quantity(result);
    }

    public static bool operator >(Quantity left, Quantity right) => left.Value > right.Value;
    public static bool operator <(Quantity left, Quantity right) => left.Value < right.Value;
    public static bool operator >=(Quantity left, Quantity right) => left.Value >= right.Value;
    public static bool operator <=(Quantity left, Quantity right) => left.Value <= right.Value;

    public bool IsZero => Value == 0;

    public override string ToString() => Value.ToString("N2");
}
