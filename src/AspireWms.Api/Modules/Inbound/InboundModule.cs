using AspireWms.Api.Shared.Contracts;

namespace AspireWms.Api.Modules.Inbound;

/// <summary>
/// Inbound module - manages purchase orders and receiving.
/// </summary>
public sealed class InboundModule : IModule
{
    public static IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Module-specific services will be registered here
        // e.g., services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        
        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/inbound")
            .WithTags("Inbound");

        group.MapGet("/health", () => Results.Ok(new 
        { 
            module = "inbound", 
            status = "healthy",
            timestamp = DateTime.UtcNow
        }));

        // Feature endpoints will be added here as vertical slices
        // e.g., PurchaseOrders.MapEndpoints(group);
        //       Receiving.MapEndpoints(group);

        return endpoints;
    }
}
