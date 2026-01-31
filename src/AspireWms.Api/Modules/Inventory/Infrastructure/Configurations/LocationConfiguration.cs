using AspireWms.Api.Modules.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inventory.Infrastructure.Configurations;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(l => l.Code)
            .HasColumnName("code")
            .HasMaxLength(15)
            .IsRequired();

        builder.HasIndex(l => l.Code)
            .IsUnique()
            .HasDatabaseName("ix_locations_code");

        builder.Property(l => l.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Zone)
            .HasColumnName("zone")
            .HasMaxLength(1)
            .IsRequired();

        builder.HasIndex(l => l.Zone)
            .HasDatabaseName("ix_locations_zone");

        builder.Property(l => l.Aisle)
            .HasColumnName("aisle");

        builder.Property(l => l.Rack)
            .HasColumnName("rack");

        builder.Property(l => l.Bin)
            .HasColumnName("bin");

        builder.Property(l => l.Capacity)
            .HasColumnName("capacity")
            .HasDefaultValue(100);

        builder.Property(l => l.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        // Composite index for location hierarchy queries
        builder.HasIndex(l => new { l.Zone, l.Aisle, l.Rack, l.Bin })
            .HasDatabaseName("ix_locations_hierarchy");

        builder.HasQueryFilter(l => l.IsActive);
    }
}
