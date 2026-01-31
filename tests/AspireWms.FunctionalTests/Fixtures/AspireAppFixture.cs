using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace AspireWms.FunctionalTests.Fixtures;

/// <summary>
/// Fixture that manages the Aspire AppHost lifecycle for functional tests.
/// Uses DistributedApplicationTestingBuilder to launch the full distributed app.
/// </summary>
public class AspireAppFixture : IAsyncDisposable
{
    private DistributedApplication? _app;
    private bool _initialized;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        
        // Create and start the AppHost using Aspire's testing builder
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AspireWms_AppHost>();

        _app = await builder.BuildAsync();

        // Start the distributed application (PostgreSQL, Redis, API, Gateway)
        await _app.StartAsync();
        
        // Wait for resources to be running before tests proceed
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api", cts.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("gateway", cts.Token);
        
        _initialized = true;
    }

    /// <summary>
    /// Creates an HttpClient configured to talk to a specific resource.
    /// </summary>
    public HttpClient CreateHttpClient(string resourceName)
    {
        if (_app is null)
            throw new InvalidOperationException("AppHost not initialized. Call InitializeAsync first.");

        return _app.CreateHttpClient(resourceName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
