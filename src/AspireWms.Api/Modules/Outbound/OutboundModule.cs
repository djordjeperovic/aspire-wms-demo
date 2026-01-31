using AspireWms.Api.Shared.Contracts;

namespace AspireWms.Api.Modules.Outbound;

/// <summary>
/// Outbound module - manages orders, picking, and shipping.
/// </summary>
public sealed class OutboundModule : IModule
{
    public static IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Module-specific services will be registered here
        // e.g., services.AddScoped<IOrderRepository, OrderRepository>();
        
        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/outbound")
            .WithTags("Outbound");

        group.MapGet("/health", () => Results.Ok(new 
        { 
            module = "outbound", 
            status = "healthy",
            timestamp = DateTime.UtcNow
        }));

        // Feature endpoints will be added here as vertical slices
        // e.g., Orders.MapEndpoints(group);
        //       Picking.MapEndpoints(group);
        //       Shipping.MapEndpoints(group);

        return endpoints;
    }
}
