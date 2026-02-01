namespace AspireWms.Api.Shared.Domain.ValueObjects;

/// <summary>
/// Represents the physical dimensions (Length, Width, Height) of an item in the warehouse.
/// </summary>
public sealed record Dimensions
{
    public decimal Length { get; }
    public decimal Width { get; }
    public decimal Height { get; }

    private Dimensions(decimal length, decimal width, decimal height)
    {
        Length = length;
        Width = width;
        Height = height;
    }

    public static Result<Dimensions> Create(decimal length, decimal width, decimal height)
    {
        if (length < 0)
            return Error.Validation("Dimensions.Length", "Length cannot be negative.");

        if (width < 0)
            return Error.Validation("Dimensions.Width", "Width cannot be negative.");

        if (height < 0)
            return Error.Validation("Dimensions.Height", "Height cannot be negative.");

        return new Dimensions(length, width, height);
    }

    public decimal Volume => Length * Width * Height;

    public bool IsZero => Length == 0 || Width == 0 || Height == 0;

    public override string ToString() => $"{Length:N2} × {Width:N2} × {Height:N2}";
}
