using AspireWms.Api.Modules.Inbound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireWms.Api.Modules.Inbound.Infrastructure.Configurations;

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("receipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.PurchaseOrderId)
            .HasColumnName("purchase_order_id")
            .IsRequired();

        builder.Property(r => r.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(r => r.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(r => r.PurchaseOrder)
            .WithMany()
            .HasForeignKey(r => r.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.Receipt)
            .HasForeignKey(l => l.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
