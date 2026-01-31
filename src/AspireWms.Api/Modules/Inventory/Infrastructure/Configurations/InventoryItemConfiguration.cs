using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inventory.Infrastructure.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(i => i.LocationId)
            .HasColumnName("location_id")
            .IsRequired();

        // Unique constraint: only one inventory item per product-location combination
        builder.HasIndex(i => new { i.ProductId, i.LocationId })
            .IsUnique()
            .HasDatabaseName("ix_inventory_items_product_location");

        // Foreign keys
        builder.HasOne(i => i.Product)
            .WithMany(p => p.InventoryItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Location)
            .WithMany(l => l.InventoryItems)
            .HasForeignKey(i => i.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Value object conversion for Quantity
        builder.Property(i => i.Quantity)
            .HasColumnName("quantity")
            .HasConversion(
                q => q.Value,
                v => Quantity.Create(v).Value)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at");

        // Navigation for movements
        builder.HasMany(i => i.Movements)
            .WithOne(m => m.InventoryItem)
            .HasForeignKey(m => m.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
