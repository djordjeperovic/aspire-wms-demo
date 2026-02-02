using AspireWms.Api.Modules.Inbound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inbound.Infrastructure.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.OrderNumber)
            .HasColumnName("order_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => p.OrderNumber)
            .IsUnique()
            .HasDatabaseName("ix_purchase_orders_order_number");

        builder.Property(p => p.SupplierName)
            .HasColumnName("supplier_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(40)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.ExpectedDeliveryDate)
            .HasColumnName("expected_delivery_date")
            .HasColumnType("date");

        builder.Property(p => p.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.PurchaseOrder)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
