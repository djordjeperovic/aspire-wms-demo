using AspireWms.Api.Modules.Inbound.Domain.Entities;
using AspireWms.Api.Shared.Domain.ValueObjects;

namespace AspireWms.UnitTests.Modules.Inbound.Domain.Entities;

public sealed class PurchaseOrderTests
{
    [Test]
    public async Task Create_NormalizesOrderNumber_ToUpperCase()
    {
        // Act
        var result = PurchaseOrder.Create("po-1001", "Supplier");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.OrderNumber).IsEqualTo("PO-1001");
        await Assert.That(result.Value.Status).IsEqualTo(PurchaseOrderStatus.Draft);
    }

    [Test]
    public async Task AddLine_WithDuplicateProduct_ReturnsFailure()
    {
        // Arrange
        var order = PurchaseOrder.Create("PO-2001", "Supplier").Value;
        var productId = Guid.NewGuid();
        var quantity = Quantity.Create(5).Value;
        var unitCost = Money.Create(3).Value;

        // Act
        var first = order.AddLine(productId, quantity, unitCost);
        var second = order.AddLine(productId, quantity, unitCost);

        // Assert
        await Assert.That(first.IsSuccess).IsTrue();
        await Assert.That(second.IsFailure).IsTrue();
    }

    [Test]
    public async Task Submit_WhenDraft_UpdatesStatus()
    {
        // Arrange
        var order = PurchaseOrder.Create("PO-3001", "Supplier").Value;

        // Act
        var result = order.Submit();

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(order.Status).IsEqualTo(PurchaseOrderStatus.Submitted);
    }

    [Test]
    public async Task ApplyReceipt_WithPartialQuantities_SetsPartiallyReceived()
    {
        // Arrange
        var order = PurchaseOrder.Create("PO-4001", "Supplier").Value;
        var line1 = order.AddLine(Guid.NewGuid(), Quantity.Create(10).Value, Money.Create(2).Value).Value;
        order.AddLine(Guid.NewGuid(), Quantity.Create(5).Value, Money.Create(3).Value);

        // Act
        var result = order.ApplyReceipt([(line1.Id, Quantity.Create(4).Value)]);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(order.Status).IsEqualTo(PurchaseOrderStatus.PartiallyReceived);
    }

    [Test]
    public async Task ApplyReceipt_WithAllLinesFulfilled_SetsFullyReceived()
    {
        // Arrange
        var order = PurchaseOrder.Create("PO-5001", "Supplier").Value;
        var line1 = order.AddLine(Guid.NewGuid(), Quantity.Create(2).Value, Money.Create(1).Value).Value;
        var line2 = order.AddLine(Guid.NewGuid(), Quantity.Create(3).Value, Money.Create(1).Value).Value;

        // Act
        var first = order.ApplyReceipt([(line1.Id, Quantity.Create(2).Value)]);
        var second = order.ApplyReceipt([(line2.Id, Quantity.Create(3).Value)]);

        // Assert
        await Assert.That(first.IsSuccess).IsTrue();
        await Assert.That(second.IsSuccess).IsTrue();
        await Assert.That(order.Status).IsEqualTo(PurchaseOrderStatus.FullyReceived);
    }

    [Test]
    public async Task Create_WithEmptyOrderNumber_ReturnsFailure()
    {
        var result = PurchaseOrder.Create("", "Supplier");
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Order number");
    }

    [Test]
    public async Task Create_WithEmptySupplierName_ReturnsFailure()
    {
        var result = PurchaseOrder.Create("PO-001", "");
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Supplier name");
    }

    [Test]
    public async Task Cancel_FromDraft_Succeeds()
    {
        var order = PurchaseOrder.Create("PO-C1", "Supplier").Value;
        var result = order.Cancel();
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(order.Status).IsEqualTo(PurchaseOrderStatus.Cancelled);
    }

    [Test]
    public async Task Cancel_FromSubmitted_Succeeds()
    {
        var order = PurchaseOrder.Create("PO-C2", "Supplier").Value;
        order.Submit();
        var result = order.Cancel();
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(order.Status).IsEqualTo(PurchaseOrderStatus.Cancelled);
    }

    [Test]
    public async Task Cancel_FromFullyReceived_Fails()
    {
        var order = PurchaseOrder.Create("PO-C3", "Supplier").Value;
        var line = order.AddLine(Guid.NewGuid(), Quantity.Create(1).Value, Money.Create(1).Value).Value;
        order.ApplyReceipt([(line.Id, Quantity.Create(1).Value)]);

        var result = order.Cancel();
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Cancel_AlreadyCancelled_Fails()
    {
        var order = PurchaseOrder.Create("PO-C4", "Supplier").Value;
        order.Cancel();
        var result = order.Cancel();
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Submit_AlreadySubmitted_Fails()
    {
        var order = PurchaseOrder.Create("PO-S1", "Supplier").Value;
        order.Submit();
        var result = order.Submit();
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Submit_CancelledOrder_Fails()
    {
        var order = PurchaseOrder.Create("PO-S2", "Supplier").Value;
        order.Cancel();
        var result = order.Submit();
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task AddLine_ToCancelledOrder_Fails()
    {
        var order = PurchaseOrder.Create("PO-AL1", "Supplier").Value;
        order.Cancel();
        var result = order.AddLine(Guid.NewGuid(), Quantity.Create(1).Value, Money.Create(1).Value);
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task ApplyReceipt_ToCancelledOrder_Fails()
    {
        var order = PurchaseOrder.Create("PO-AR1", "Supplier").Value;
        var line = order.AddLine(Guid.NewGuid(), Quantity.Create(5).Value, Money.Create(1).Value).Value;
        order.Cancel();

        var result = order.ApplyReceipt([(line.Id, Quantity.Create(1).Value)]);
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task ApplyReceipt_WithNonExistentLineId_Fails()
    {
        var order = PurchaseOrder.Create("PO-AR2", "Supplier").Value;
        order.AddLine(Guid.NewGuid(), Quantity.Create(5).Value, Money.Create(1).Value);

        var result = order.ApplyReceipt([(Guid.NewGuid(), Quantity.Create(1).Value)]);
        await Assert.That(result.IsFailure).IsTrue();
    }
}
