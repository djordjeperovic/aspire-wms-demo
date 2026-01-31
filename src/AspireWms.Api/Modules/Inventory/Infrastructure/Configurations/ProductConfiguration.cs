using AspireWms.Api.Modules.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inventory.Infrastructure.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Sku)
            .HasColumnName("sku")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("ix_products_sku");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(p => p.Weight)
            .HasColumnName("weight")
            .HasPrecision(10, 3);

        builder.Property(p => p.Length)
            .HasColumnName("length")
            .HasPrecision(10, 3);

        builder.Property(p => p.Width)
            .HasColumnName("width")
            .HasPrecision(10, 3);

        builder.Property(p => p.Height)
            .HasColumnName("height")
            .HasPrecision(10, 3);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        // Filter for active products by default (can be overridden with IgnoreQueryFilters)
        builder.HasQueryFilter(p => p.IsActive);
    }
}
