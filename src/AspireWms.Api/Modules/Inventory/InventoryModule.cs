using AspireWms.Api.Modules.Inventory.Features.Locations;
using AspireWms.Api.Modules.Inventory.Features.Products;
using AspireWms.Api.Modules.Inventory.Features.Stock;
using AspireWms.Api.Modules.Inventory.Infrastructure;
using AspireWms.Api.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AspireWms.Api.Modules.Inventory;

/// <summary>
/// Inventory module - manages products, locations, and stock levels.
/// </summary>
public sealed class InventoryModule : IModule
{
    public static IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext with Npgsql (connection string from Aspire)
        // All modules share the same database (wmsdb) but use separate schemas
        services.AddDbContext<InventoryDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("wmsdb");
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "inventory");
            });
        });

        // Register Redis caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("redis");
            options.InstanceName = "inventory:";
        });

        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/inventory")
            .WithTags("Inventory");

        group.MapGet("/health", () => Results.Ok(new 
        { 
            module = "inventory", 
            status = "healthy",
            timestamp = DateTime.UtcNow
        }));

        // Feature endpoints (vertical slices)
        ProductEndpoints.Map(group);
        LocationEndpoints.Map(group);
        StockEndpoints.Map(group);

        return endpoints;
    }
}
