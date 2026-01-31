using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AspireWms.Api.Modules.Inventory.Features.Locations;

// === DTOs ===
public sealed record LocationDto(
    Guid Id,
    string Code,
    string Name,
    string Zone,
    int Aisle,
    int Rack,
    int Bin,
    int Capacity,
    bool IsActive,
    DateTime CreatedAt);

// === List Locations ===
public sealed record ListLocationsQuery(string? Zone = null, bool IncludeInactive = false) 
    : IRequest<IReadOnlyList<LocationDto>>;

public sealed class ListLocationsHandler(InventoryDbContext db)
    : IRequestHandler<ListLocationsQuery, IReadOnlyList<LocationDto>>
{
    public async Task<IReadOnlyList<LocationDto>> Handle(ListLocationsQuery request, CancellationToken cancellationToken)
    {
        var query = request.IncludeInactive
            ? db.Locations.IgnoreQueryFilters()
            : db.Locations;

        if (!string.IsNullOrWhiteSpace(request.Zone))
        {
            query = query.Where(l => l.Zone == request.Zone.ToUpperInvariant());
        }

        return await query
            .OrderBy(l => l.Zone)
            .ThenBy(l => l.Aisle)
            .ThenBy(l => l.Rack)
            .ThenBy(l => l.Bin)
            .Select(l => new LocationDto(
                l.Id,
                l.Code,
                l.Name,
                l.Zone,
                l.Aisle,
                l.Rack,
                l.Bin,
                l.Capacity,
                l.IsActive,
                l.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// === Create Location ===
public sealed record CreateLocationCommand(
    string Zone,
    int Aisle,
    int Rack,
    int Bin,
    string? Name = null,
    int Capacity = 100) : IRequest<CreateLocationResult>;

public sealed record CreateLocationResult(bool Success, Guid? Id = null, string? Code = null, string? Error = null);

public sealed class CreateLocationHandler(InventoryDbContext db)
    : IRequestHandler<CreateLocationCommand, CreateLocationResult>
{
    public async Task<CreateLocationResult> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        var locationResult = Location.Create(
            request.Zone,
            request.Aisle,
            request.Rack,
            request.Bin,
            request.Name,
            request.Capacity);

        if (locationResult.IsFailure)
        {
            return new CreateLocationResult(false, Error: locationResult.Error.Message);
        }

        var location = locationResult.Value;

        // Check for duplicate code
        var existingCode = await db.Locations
            .IgnoreQueryFilters()
            .AnyAsync(l => l.Code == location.Code, cancellationToken);

        if (existingCode)
        {
            return new CreateLocationResult(false, Error: $"Location with code '{location.Code}' already exists.");
        }

        db.Locations.Add(location);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateLocationResult(true, location.Id, location.Code);
    }
}

// === Endpoints ===
public static class LocationEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        var locations = group.MapGroup("/locations").WithTags("Locations");

        locations.MapGet("/", async (IMediator mediator, string? zone = null, bool includeInactive = false) =>
        {
            var result = await mediator.Send(new ListLocationsQuery(zone, includeInactive));
            return Results.Ok(result);
        })
        .WithName("ListLocations")
        .WithSummary("List all locations, optionally filtered by zone");

        locations.MapPost("/", async (CreateLocationCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.Success
                ? Results.Created($"/inventory/locations/{result.Id}", new { id = result.Id, code = result.Code })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CreateLocation")
        .WithSummary("Create a new warehouse location");
    }
}
