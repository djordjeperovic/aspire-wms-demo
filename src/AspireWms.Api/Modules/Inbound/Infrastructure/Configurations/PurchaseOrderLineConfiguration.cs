using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inbound.Infrastructure.Configurations;

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("purchase_order_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(l => l.PurchaseOrderId)
            .HasColumnName("purchase_order_id")
            .IsRequired();

        builder.Property(l => l.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.HasIndex(l => new { l.PurchaseOrderId, l.ProductId })
            .IsUnique()
            .HasDatabaseName("ix_purchase_order_lines_order_product");

        builder.Property(l => l.Quantity)
            .HasColumnName("quantity")
            .HasConversion(
                q => q.Value,
                v => Quantity.Create(v).Value)
            .IsRequired();

        builder.Property(l => l.ReceivedQuantity)
            .HasColumnName("received_quantity")
            .HasConversion(
                q => q.Value,
                v => Quantity.Create(v).Value)
            .IsRequired();

        builder.OwnsOne(l => l.UnitCost, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("unit_cost_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("unit_cost_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
