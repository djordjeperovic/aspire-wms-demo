using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Infrastructure;
using AspireWms.Api.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AspireWms.Api.Modules.Inbound.Infrastructure;

public static class InboundDbSeeder
{
    public static async Task SeedAsync(InboundDbContext inboundDb, InventoryDbContext inventoryDb)
    {
        if (await inboundDb.PurchaseOrders.AnyAsync())
            return;

        var products = await inventoryDb.Products
            .IgnoreQueryFilters()
            .OrderBy(p => p.Sku)
            .Take(5)
            .ToListAsync();

        if (products.Count < 3)
            return;

        var draftPo = PurchaseOrder.Create(
            "PO-1001",
            "Acme Supplies",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            "Draft inbound order");

        if (draftPo.IsFailure)
            return;

        var submittedPo = PurchaseOrder.Create(
            "PO-1002",
            "Northwind Traders",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)),
            "Submitted order awaiting receipt");

        if (submittedPo.IsFailure)
            return;

        var cancelledPo = PurchaseOrder.Create(
            "PO-1003",
            "Fabrikam Logistics",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            "Cancelled order for demo data");

        if (cancelledPo.IsFailure)
            return;

        if (draftPo.Value.AddLine(products[0].Id, Quantity.Create(20).Value, Money.Create(12.50m).Value).IsFailure)
            return;
        if (draftPo.Value.AddLine(products[1].Id, Quantity.Create(10).Value, Money.Create(35.00m).Value).IsFailure)
            return;

        if (submittedPo.Value.AddLine(products[2].Id, Quantity.Create(15).Value, Money.Create(19.99m).Value).IsFailure)
            return;
        if (submittedPo.Value.AddLine(products[3].Id, Quantity.Create(8).Value, Money.Create(42.00m).Value).IsFailure)
            return;

        var submitResult = submittedPo.Value.Submit();
        if (submitResult.IsFailure)
            return;

        if (cancelledPo.Value.AddLine(products[4].Id, Quantity.Create(5).Value, Money.Create(99.00m).Value).IsFailure)
            return;
        if (cancelledPo.Value.Cancel().IsFailure)
            return;

        var receiptLineTarget = submittedPo.Value.Lines.First();
        var receivedQty = Quantity.Create(5).Value;
        if (submittedPo.Value.ApplyReceipt([(receiptLineTarget.Id, receivedQty)]).IsFailure)
            return;

        var receiptLine = ReceiptLine.Create(
            receiptLineTarget.Id,
            receiptLineTarget.ProductId,
            receivedQty,
            receiptLineTarget.UnitCost);

        if (receiptLine.IsFailure)
            return;

        var receipt = Receipt.Create(
            submittedPo.Value.Id,
            DateTime.UtcNow.AddDays(-1),
            "Partial delivery",
            [receiptLine.Value]);

        if (receipt.IsFailure)
            return;

        inboundDb.PurchaseOrders.AddRange(draftPo.Value, submittedPo.Value, cancelledPo.Value);
        inboundDb.Receipts.Add(receipt.Value);

        await inboundDb.SaveChangesAsync();
    }
}
