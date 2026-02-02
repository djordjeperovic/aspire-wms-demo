using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inbound.Infrastructure.Configurations;

public sealed class ReceiptLineConfiguration : IEntityTypeConfiguration<ReceiptLine>
{
    public void Configure(EntityTypeBuilder<ReceiptLine> builder)
    {
        builder.ToTable("receipt_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(l => l.ReceiptId)
            .HasColumnName("receipt_id")
            .IsRequired();

        builder.Property(l => l.PurchaseOrderLineId)
            .HasColumnName("purchase_order_line_id")
            .IsRequired();

        builder.Property(l => l.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(l => l.QuantityReceived)
            .HasColumnName("quantity_received")
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

        builder.HasOne(l => l.Receipt)
            .WithMany(r => r.Lines)
            .HasForeignKey(l => l.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PurchaseOrderLine>()
            .WithMany()
            .HasForeignKey(l => l.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
