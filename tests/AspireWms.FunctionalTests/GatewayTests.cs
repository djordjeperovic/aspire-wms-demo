using System.Net;
using AspireWms.FunctionalTests.Fixtures;
using Polly;
using Polly.Retry;

namespace AspireWms.FunctionalTests;

/// <summary>
/// Functional tests for the YARP API Gateway.
/// Tests that routes correctly forward requests to the API.
/// </summary>
public class GatewayTests
{
    private static readonly AspireAppFixture _fixture = new();
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
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
    public async Task Gateway_InventoryHealthEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");

        // Act - with retry for container startup
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync("/api/inventory/health"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Gateway_InboundHealthEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync("/api/inbound/health"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Gateway_OutboundHealthEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync("/api/outbound/health"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Gateway_ApiRootEndpoint_ReturnsServiceInfo()
    {
        // Arrange
        var client = _fixture.CreateHttpClient("gateway");

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync("/api"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("AspireWms.Api");
    }

    [Test]
    public async Task Api_DirectHealthEndpoint_ReturnsOk()
    {
        // Arrange - test API directly (bypassing gateway)
        var client = _fixture.CreateHttpClient("api");

        // Act
        var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync("/health"));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
