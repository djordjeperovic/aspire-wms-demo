using AspireWms.Api.Shared.Domain;

namespace AspireWms.Api.Modules.Inventory.Domain.Entities;

/// <summary>
/// Represents a product in the warehouse inventory.
/// </summary>
public sealed class Product : Entity<Guid>
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Weight { get; private set; }
    public decimal Length { get; private set; }
    public decimal Width { get; private set; }
    public decimal Height { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<InventoryItem> _inventoryItems = [];
    public IReadOnlyCollection<InventoryItem> InventoryItems => _inventoryItems.AsReadOnly();

    private Product() : base() { }

    private Product(Guid id, string sku, string name, string? description,
        decimal weight, decimal length, decimal width, decimal height)
        : base(id)
    {
        Sku = sku;
        Name = name;
        Description = description;
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        IsActive = true;
    }

    public static Result<Product> Create(
        string sku,
        string name,
        string? description = null,
        decimal weight = 0,
        decimal length = 0,
        decimal width = 0,
        decimal height = 0)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return Error.Validation("Product.Sku", "SKU is required.");

        if (sku.Length > 50)
            return Error.Validation("Product.Sku", "SKU cannot exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Product.Name", "Name is required.");

        if (name.Length > 200)
            return Error.Validation("Product.Name", "Name cannot exceed 200 characters.");

        if (weight < 0)
            return Error.Validation("Product.Weight", "Weight cannot be negative.");

        if (length < 0 || width < 0 || height < 0)
            return Error.Validation("Product.Dimensions", "Dimensions cannot be negative.");

        return new Product(
            Guid.NewGuid(),
            sku.Trim().ToUpperInvariant(),
            name.Trim(),
            description?.Trim(),
            weight,
            length,
            width,
            height);
    }

    public Result Update(
        string name,
        string? description,
        decimal weight,
        decimal length,
        decimal width,
        decimal height)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Product.Name", "Name is required.");

        if (name.Length > 200)
            return Error.Validation("Product.Name", "Name cannot exceed 200 characters.");

        if (weight < 0)
            return Error.Validation("Product.Weight", "Weight cannot be negative.");

        if (length < 0 || width < 0 || height < 0)
            return Error.Validation("Product.Dimensions", "Dimensions cannot be negative.");

        Name = name.Trim();
        Description = description?.Trim();
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        MarkUpdated();

        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    /// <summary>
    /// Volume in cubic units (length × width × height).
    /// </summary>
    public decimal Volume => Length * Width * Height;
}
