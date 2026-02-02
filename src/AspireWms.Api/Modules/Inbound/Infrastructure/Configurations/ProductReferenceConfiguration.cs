using AspireWms.Api.Modules.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inbound.Infrastructure.Configurations;

public sealed class ProductReferenceConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "inventory", table => table.ExcludeFromMigrations());

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Ignore(p => p.Sku);
        builder.Ignore(p => p.Name);
        builder.Ignore(p => p.Description);
        builder.Ignore(p => p.Weight);
        builder.Ignore(p => p.Dimensions);
        builder.Ignore(p => p.IsActive);
        builder.Ignore(p => p.InventoryItems);
        builder.Ignore(p => p.CreatedAt);
        builder.Ignore(p => p.UpdatedAt);
        builder.Ignore(p => p.Volume);
    }
}
