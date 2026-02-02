using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspireWms.FunctionalTests.Fixtures;
using Polly;
using Polly.Retry;

namespace AspireWms.FunctionalTests;

/// <summary>
/// Functional tests for the Inbound module endpoints through the gateway.
/// </summary>
public sealed class InboundEndpointsTests
{
    private static readonly AspireAppFixture _fixture = new();
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .Or<HttpRequestException>()
        .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(2));

    [Before(Class)]
    public static async Task SetupFixture()
    {
        await _fixture.InitializeAsync();
    }

    [After(Class)]
    public static async Task TeardownFixture()
    {
        await _fixture.DisposeAsync();
    }

    [Test]
    public async Task ListPurchaseOrders_ReturnsSeededOrders()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync("/api/inbound/purchase-orders"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("PO-1001");
    }

    [Test]
    public async Task CreatePurchaseOrder_ReturnsCreatedAndCanBeFetched()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        var productIds = await FetchProductIds(client, 2);
        var orderNumber = $"PO-{Guid.NewGuid():N}";

        var payload = new
        {
            OrderNumber = orderNumber,
            SupplierName = "Test Supplier",
            Notes = "Test purchase order",
            Lines = new[]
            {
                new { ProductId = productIds[0], Quantity = 5, UnitCostAmount = 12.50m, UnitCostCurrency = "USD" },
                new { ProductId = productIds[1], Quantity = 3, UnitCostAmount = 8.75m, UnitCostCurrency = "USD" }
            }
        };

        // Act
        var createResponse = await _retryPolicy.ExecuteAsync(() =>
            client.PostAsJsonAsync("/api/inbound/purchase-orders", payload));

        // Assert
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var createdContent = await createResponse.Content.ReadAsStringAsync();

        using var createdDoc = JsonDocument.Parse(createdContent);
        var purchaseOrderId = createdDoc.RootElement.GetProperty("id").GetGuid();

        var getResponse = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync($"/api/inbound/purchase-orders/{purchaseOrderId}"));

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getContent);
        var returnedOrderNumber = getDoc.RootElement.GetProperty("orderNumber").GetString();
        await Assert.That(returnedOrderNumber).IsEqualTo(orderNumber.ToUpperInvariant());
    }

    [Test]
    public async Task CreateReceipt_UpdatesPurchaseOrderStatus()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        var productIds = await FetchProductIds(client, 2);
        var orderNumber = $"PO-{Guid.NewGuid():N}";

        var purchaseOrderPayload = new
        {
            OrderNumber = orderNumber,
            SupplierName = "Receipt Supplier",
            Lines = new[]
            {
                new { ProductId = productIds[0], Quantity = 4, UnitCostAmount = 10m, UnitCostCurrency = "USD" },
                new { ProductId = productIds[1], Quantity = 2, UnitCostAmount = 15m, UnitCostCurrency = "USD" }
            }
        };

        var createResponse = await _retryPolicy.ExecuteAsync(() =>
            client.PostAsJsonAsync("/api/inbound/purchase-orders", purchaseOrderPayload));

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var createdContent = await createResponse.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createdContent);
        var purchaseOrderId = createdDoc.RootElement.GetProperty("id").GetGuid();

        var orderResponse = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync($"/api/inbound/purchase-orders/{purchaseOrderId}"));
        var orderContent = await orderResponse.Content.ReadAsStringAsync();
        using var orderDoc = JsonDocument.Parse(orderContent);
        var lines = orderDoc.RootElement.GetProperty("lines").EnumerateArray().ToList();

        var receiptPayload = new
        {
            PurchaseOrderId = purchaseOrderId,
            ReceivedAt = DateTime.UtcNow,
            Notes = "Partial receipt",
            Lines = new[]
            {
                new
                {
                    PurchaseOrderLineId = lines[0].GetProperty("id").GetGuid(),
                    QuantityReceived = 2
                }
            }
        };

        // Act
        var receiptResponse = await _retryPolicy.ExecuteAsync(() =>
            client.PostAsJsonAsync("/api/inbound/receipts", receiptPayload));

        // Assert
        await Assert.That(receiptResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var updatedOrderResponse = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync($"/api/inbound/purchase-orders/{purchaseOrderId}"));
        var updatedContent = await updatedOrderResponse.Content.ReadAsStringAsync();

        await Assert.That(updatedContent).Contains("PartiallyReceived");
    }

    [Test]
    public async Task GetPurchaseOrderById_ReturnsOkWithLines()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        var productIds = await FetchProductIds(client, 1);
        var orderNumber = $"PO-{Guid.NewGuid():N}";

        var payload = new
        {
            OrderNumber = orderNumber,
            SupplierName = "Get By Id Supplier",
            Lines = new[] { new { ProductId = productIds[0], Quantity = 3, UnitCostAmount = 7m, UnitCostCurrency = "USD" } }
        };

        var createResponse = await _retryPolicy.ExecuteAsync(() =>
            client.PostAsJsonAsync("/api/inbound/purchase-orders", payload));
        var createdContent = await createResponse.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createdContent);
        var poId = createdDoc.RootElement.GetProperty("id").GetGuid();

        // Act
        var response = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync($"/api/inbound/purchase-orders/{poId}"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var lines = doc.RootElement.GetProperty("lines").GetArrayLength();
        await Assert.That(lines).IsEqualTo(1);
    }

    [Test]
    public async Task GetNonExistentPurchaseOrder_Returns404()
    {
        var client = _fixture.CreateHttpClient("gateway");
        var response = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync($"/api/inbound/purchase-orders/{Guid.NewGuid()}"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreatePurchaseOrder_DuplicateOrderNumber_Returns400()
    {
        var client = _fixture.CreateHttpClient("gateway");
        var productIds = await FetchProductIds(client, 1);
        var orderNumber = $"PO-{Guid.NewGuid():N}";

        var payload = new
        {
            OrderNumber = orderNumber,
            SupplierName = "Dup Supplier",
            Lines = new[] { new { ProductId = productIds[0], Quantity = 1, UnitCostAmount = 1m, UnitCostCurrency = "USD" } }
        };

        var first = await _retryPolicy.ExecuteAsync(() =>
            client.PostAsJsonAsync("/api/inbound/purchase-orders", payload));
        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var second = await _retryPolicy.ExecuteAsync(() =>
            client.PostAsJsonAsync("/api/inbound/purchase-orders", payload));
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ListReceipts_ReturnsOk()
    {
        var client = _fixture.CreateHttpClient("gateway");
        var response = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync("/api/inbound/receipts"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetReceiptById_ReturnsOkWithLines()
    {
        // Arrange - create PO then receipt
        var client = _fixture.CreateHttpClient("gateway");
        var productIds = await FetchProductIds(client, 1);
        var orderNumber = $"PO-{Guid.NewGuid():N}";

        var poPayload = new
        {
            OrderNumber = orderNumber,
            SupplierName = "Receipt Supplier",
            Lines = new[] { new { ProductId = productIds[0], Quantity = 5, UnitCostAmount = 10m, UnitCostCurrency = "USD" } }
        };

        var createPoResp = await _retryPolicy.ExecuteAsync(() =>
            client.PostAsJsonAsync("/api/inbound/purchase-orders", poPayload));
        var poContent = await createPoResp.Content.ReadAsStringAsync();
        using var poDoc = JsonDocument.Parse(poContent);
        var poId = poDoc.RootElement.GetProperty("id").GetGuid();

        var orderResp = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync($"/api/inbound/purchase-orders/{poId}"));
        var orderContent = await orderResp.Content.ReadAsStringAsync();
        using var orderDoc = JsonDocument.Parse(orderContent);
        var lineId = orderDoc.RootElement.GetProperty("lines")[0].GetProperty("id").GetGuid();

        var receiptPayload = new
        {
            PurchaseOrderId = poId,
            ReceivedAt = DateTime.UtcNow,
            Lines = new[] { new { PurchaseOrderLineId = lineId, QuantityReceived = 2 } }
        };

        var createReceiptResp = await _retryPolicy.ExecuteAsync(() =>
            client.PostAsJsonAsync("/api/inbound/receipts", receiptPayload));
        var receiptContent = await createReceiptResp.Content.ReadAsStringAsync();
        using var receiptDoc = JsonDocument.Parse(receiptContent);
        var receiptId = receiptDoc.RootElement.GetProperty("id").GetGuid();

        // Act
        var response = await _retryPolicy.ExecuteAsync(() =>
            client.GetAsync($"/api/inbound/receipts/{receiptId}"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var bodyDoc = JsonDocument.Parse(body);
        var receiptLines = bodyDoc.RootElement.GetProperty("lines").GetArrayLength();
        await Assert.That(receiptLines).IsEqualTo(1);
    }

    private static async Task<List<Guid>> FetchProductIds(HttpClient client, int count)
    {
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync("/api/inventory/products"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        return doc.RootElement.EnumerateArray()
            .Take(count)
            .Select(element => element.GetProperty("id").GetGuid())
            .ToList();
    }
}
