using AspireWms.Api.Modules.Inbound;
using AspireWms.Api.Modules.Inbound.Infrastructure;
using AspireWms.Api.Modules.Inventory;
using AspireWms.Api.Modules.Inventory.Infrastructure;
using AspireWms.Api.Modules.Outbound;
using AspireWms.Api.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, resilience)
builder.AddServiceDefaults();

// Add shared infrastructure (MediatR, FluentValidation, behaviors)
builder.Services.AddSharedInfrastructure();

// Register module services
InventoryModule.RegisterServices(builder.Services, builder.Configuration);
InboundModule.RegisterServices(builder.Services, builder.Configuration);
OutboundModule.RegisterServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Apply migrations and seed data
await using (var scope = app.Services.CreateAsyncScope())
{
    var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await inventoryDb.Database.MigrateAsync();
    await InventoryDbSeeder.SeedAsync(inventoryDb);

    var inboundDb = scope.ServiceProvider.GetRequiredService<InboundDbContext>();
    await inboundDb.Database.MigrateAsync();
    await InboundDbSeeder.SeedAsync(inboundDb, inventoryDb);
}

// Map default health endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Map module endpoints
InventoryModule.MapEndpoints(app);
InboundModule.MapEndpoints(app);
OutboundModule.MapEndpoints(app);

// Root API info endpoint
app.MapGet("/api", () => Results.Ok(new { 
    service = "AspireWms.Api", 
    version = "1.0.0",
    modules = new[] { "inventory", "inbound", "outbound" }
}));

app.Run();
