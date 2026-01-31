using System.Text.RegularExpressions;
using AspireWms.Api.Shared.Domain;

namespace AspireWms.Api.Modules.Inventory.Domain.Entities;

/// <summary>
/// Represents a warehouse storage location.
/// Code format: Zone-Aisle-Rack-Bin (e.g., "A-01-02-03")
/// </summary>
public sealed partial class Location : Entity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Zone { get; private set; } = string.Empty;
    public int Aisle { get; private set; }
    public int Rack { get; private set; }
    public int Bin { get; private set; }
    public int Capacity { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<InventoryItem> _inventoryItems = [];
    public IReadOnlyCollection<InventoryItem> InventoryItems => _inventoryItems.AsReadOnly();

    private Location() : base() { }

    private Location(Guid id, string code, string name, string zone, int aisle, int rack, int bin, int capacity)
        : base(id)
    {
        Code = code;
        Name = name;
        Zone = zone;
        Aisle = aisle;
        Rack = rack;
        Bin = bin;
        Capacity = capacity;
        IsActive = true;
    }

    /// <summary>
    /// Creates a location from individual components.
    /// </summary>
    public static Result<Location> Create(
        string zone,
        int aisle,
        int rack,
        int bin,
        string? name = null,
        int capacity = 100)
    {
        if (string.IsNullOrWhiteSpace(zone) || zone.Length != 1 || !char.IsLetter(zone[0]))
            return Error.Validation("Location.Zone", "Zone must be a single letter (A-Z).");

        if (aisle < 1 || aisle > 99)
            return Error.Validation("Location.Aisle", "Aisle must be between 1 and 99.");

        if (rack < 1 || rack > 99)
            return Error.Validation("Location.Rack", "Rack must be between 1 and 99.");

        if (bin < 1 || bin > 99)
            return Error.Validation("Location.Bin", "Bin must be between 1 and 99.");

        if (capacity < 1)
            return Error.Validation("Location.Capacity", "Capacity must be at least 1.");

        var normalizedZone = zone.ToUpperInvariant();
        var code = $"{normalizedZone}-{aisle:D2}-{rack:D2}-{bin:D2}";
        var defaultName = name?.Trim() ?? $"Zone {normalizedZone}, Aisle {aisle}, Rack {rack}, Bin {bin}";

        return new Location(
            Guid.NewGuid(),
            code,
            defaultName,
            normalizedZone,
            aisle,
            rack,
            bin,
            capacity);
    }

    /// <summary>
    /// Creates a location from a code string (e.g., "A-01-02-03").
    /// </summary>
    public static Result<Location> CreateFromCode(string code, string? name = null, int capacity = 100)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Error.Validation("Location.Code", "Code is required.");

        var match = LocationCodeRegex().Match(code.Trim().ToUpperInvariant());
        if (!match.Success)
            return Error.Validation("Location.Code", "Code must be in format 'Z-AA-RR-BB' (e.g., 'A-01-02-03').");

        var zone = match.Groups[1].Value;
        var aisle = int.Parse(match.Groups[2].Value);
        var rack = int.Parse(match.Groups[3].Value);
        var bin = int.Parse(match.Groups[4].Value);

        return Create(zone, aisle, rack, bin, name, capacity);
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

    public Result UpdateCapacity(int capacity)
    {
        if (capacity < 1)
            return Error.Validation("Location.Capacity", "Capacity must be at least 1.");

        Capacity = capacity;
        MarkUpdated();
        return Result.Success();
    }

    [GeneratedRegex(@"^([A-Z])-(\d{2})-(\d{2})-(\d{2})$")]
    private static partial Regex LocationCodeRegex();
}
