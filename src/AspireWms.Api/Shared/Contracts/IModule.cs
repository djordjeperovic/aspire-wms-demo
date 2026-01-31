namespace AspireWms.Api.Shared.Contracts;

/// <summary>
/// Interface for modules that can register their services and endpoints.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Registers module-specific services with the DI container.
    /// </summary>
    static abstract IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Maps module-specific endpoints.
    /// </summary>
    static abstract IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
