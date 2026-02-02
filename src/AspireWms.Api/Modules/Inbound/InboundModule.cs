using AspireWms.Api.Modules.Inbound.Features.PurchaseOrders;
using AspireWms.Api.Modules.Inbound.Features.Receipts;
using AspireWms.Api.Modules.Inbound.Infrastructure;
using AspireWms.Api.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AspireWms.Api.Modules.Inbound;

/// <summary>
/// Inbound module - manages purchase orders and receiving.
/// </summary>
public sealed class InboundModule : IModule
{
    public static IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InboundDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("wmsdb");
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "inbound");
            });
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("redis");
            options.InstanceName = "inbound:";
        });

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

        PurchaseOrderEndpoints.Map(group);
        ReceiptEndpoints.Map(group);

        return endpoints;
    }
}
