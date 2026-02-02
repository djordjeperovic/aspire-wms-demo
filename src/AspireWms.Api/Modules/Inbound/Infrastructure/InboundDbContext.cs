using AspireWms.Api.Modules.Inbound.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspireWms.Api.Modules.Inbound.Infrastructure;

/// <summary>
/// Database context for the Inbound module.
/// </summary>
public sealed class InboundDbContext : DbContext
{
    public InboundDbContext(DbContextOptions<InboundDbContext> options) : base(options) { }

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InboundDbContext).Assembly,
            type => type.Namespace?.StartsWith("AspireWms.Api.Modules.Inbound.Infrastructure.Configurations") == true);

        modelBuilder.HasDefaultSchema("inbound");
    }
}
