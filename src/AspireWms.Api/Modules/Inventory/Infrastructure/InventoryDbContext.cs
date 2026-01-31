using AspireWms.Api.Modules.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspireWms.Api.Modules.Inventory.Infrastructure;

/// <summary>
/// Database context for the Inventory module.
/// </summary>
public sealed class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InventoryDbContext).Assembly,
            type => type.Namespace?.StartsWith("AspireWms.Api.Modules.Inventory.Infrastructure.Configurations") == true);

        // Set default schema
        modelBuilder.HasDefaultSchema("inventory");
    }
}
