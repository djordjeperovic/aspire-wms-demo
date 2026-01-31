using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Domain.Enums;
using AspireWms.Api.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inventory.Infrastructure.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.InventoryItemId)
            .HasColumnName("inventory_item_id")
            .IsRequired();

        builder.HasIndex(m => m.InventoryItemId)
            .HasDatabaseName("ix_stock_movements_inventory_item");

        builder.Property(m => m.MovementType)
            .HasColumnName("movement_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(m => m.MovementType)
            .HasDatabaseName("ix_stock_movements_type");

        // Value object conversion for Quantity
        builder.Property(m => m.Quantity)
            .HasColumnName("quantity")
            .HasConversion(
                q => q.Value,
                v => Quantity.Create(v).Value)
            .IsRequired();

        builder.Property(m => m.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at");

        // Index for time-based queries
        builder.HasIndex(m => m.CreatedAt)
            .HasDatabaseName("ix_stock_movements_created_at");
    }
}
