using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspireWms.FunctionalTests.Fixtures;
using Polly;
using Polly.Retry;

namespace AspireWms.FunctionalTests;

/// <summary>
/// Functional tests for the Inventory module endpoints.
/// </summary>
public class InventoryEndpointsTests
{
    private static readonly AspireAppFixture _fixture = new();
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .Or<HttpRequestException>()
        .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(2));

    private static readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true 
    };

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

    // === Products Tests ===

    [Test]
    public async Task ListProducts_ReturnsSeededProducts()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync("/api/inventory/products"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("SKU001"); // Seeded product
    }

    [Test]
    public async Task CreateProduct_WithValidData_ReturnsCreated()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        var uniqueSku = $"TEST-{Guid.NewGuid():N}"[..20];
        var product = new
        {
            Sku = uniqueSku,
            Name = "Test Product",
            Description = "Test description",
            Weight = 1.5m,
            Length = 10m,
            Width = 5m,
            Height = 2m
        };

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.PostAsJsonAsync("/api/inventory/products", product));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("id");
    }

    [Test]
    public async Task CreateProduct_WithDuplicateSku_ReturnsBadRequest()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        // SKU001 is seeded data
        var product = new
        {
            Sku = "SKU001",
            Name = "Duplicate Product",
            Description = "Should fail"
        };

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.PostAsJsonAsync("/api/inventory/products", product));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("already exists");
    }

    [Test]
    public async Task GetProduct_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.GetAsync($"/api/inventory/products/{nonExistentId}"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // === Locations Tests ===

    [Test]
    public async Task ListLocations_ReturnsSeededLocations()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.GetAsync("/api/inventory/locations"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("A-01-01-01"); // Seeded location
    }

    [Test]
    public async Task ListLocations_WithZoneFilter_ReturnsFilteredResults()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.GetAsync("/api/inventory/locations?zone=A"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        // Should only contain zone A locations
        await Assert.That(content).Contains("\"zone\":\"A\"");
    }

    [Test]
    public async Task CreateLocation_WithValidData_ReturnsCreated()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        var random = new Random();
        var location = new
        {
            Zone = "Z",
            Aisle = random.Next(1, 99),
            Rack = random.Next(1, 99),
            Bin = random.Next(1, 99),
            Name = "Test Location",
            Capacity = 50
        };

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.PostAsJsonAsync("/api/inventory/locations", location));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("code");
    }

    // === Stock Tests ===

    [Test]
    public async Task GetStockLevel_ForSeededProduct_ReturnsStockInfo()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        
        // First, get a product ID from the seeded data
        var productsResponse = await _retryPolicy.ExecuteAsync(() => 
            client.GetAsync("/api/inventory/products"));
        var productsContent = await productsResponse.Content.ReadAsStringAsync();
        
        // Parse to get first product ID
        using var doc = JsonDocument.Parse(productsContent);
        var firstProduct = doc.RootElement.EnumerateArray().First();
        var productId = firstProduct.GetProperty("id").GetGuid();

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.GetAsync($"/api/inventory/stock/{productId}"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("totalQuantity");
    }

    [Test]
    public async Task GetMovementHistory_ForSeededProduct_ReturnsMovements()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        
        // Get a product ID from seeded data
        var productsResponse = await _retryPolicy.ExecuteAsync(() => 
            client.GetAsync("/api/inventory/products"));
        var productsContent = await productsResponse.Content.ReadAsStringAsync();
        
        using var doc = JsonDocument.Parse(productsContent);
        var firstProduct = doc.RootElement.EnumerateArray().First();
        var productId = firstProduct.GetProperty("id").GetGuid();

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.GetAsync($"/api/inventory/stock/{productId}/movements"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        // Seeded data has initial movements
        await Assert.That(content).Contains("Initial");
    }

    [Test]
    public async Task AdjustStock_WithValidData_ReturnsNewQuantity()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");
        
        // Get product and location IDs from seeded data
        var productsResponse = await _retryPolicy.ExecuteAsync(() => 
            client.GetAsync("/api/inventory/products"));
        var productsContent = await productsResponse.Content.ReadAsStringAsync();
        
        using var prodDoc = JsonDocument.Parse(productsContent);
        var firstProduct = prodDoc.RootElement.EnumerateArray().First();
        var productId = firstProduct.GetProperty("id").GetGuid();

        var locationsResponse = await client.GetAsync("/api/inventory/locations");
        var locationsContent = await locationsResponse.Content.ReadAsStringAsync();
        
        using var locDoc = JsonDocument.Parse(locationsContent);
        var firstLocation = locDoc.RootElement.EnumerateArray().First();
        var locationId = firstLocation.GetProperty("id").GetGuid();

        var adjustment = new
        {
            ProductId = productId,
            LocationId = locationId,
            Adjustment = 5,
            Reason = "Test adjustment +5"
        };

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => 
            client.PostAsJsonAsync("/api/inventory/stock/adjust", adjustment));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("newQuantity");
    }
}
