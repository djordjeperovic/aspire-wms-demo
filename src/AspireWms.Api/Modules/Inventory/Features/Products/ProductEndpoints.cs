using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Modules.Inventory.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AspireWms.Api.Modules.Inventory.Features.Products;

// === DTOs ===
public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// === List Products ===
public sealed record ListProductsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<ProductDto>>;

public sealed class ListProductsHandler(InventoryDbContext db, IDistributedCache cache)
    : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    private const string CacheKey = "products:list";
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<IReadOnlyList<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        // Try cache first (only for active products)
        if (!request.IncludeInactive)
        {
            var cached = await cache.GetStringAsync(CacheKey, cancellationToken);
            if (cached is not null)
            {
                return JsonSerializer.Deserialize<List<ProductDto>>(cached) ?? [];
            }
        }

        var query = request.IncludeInactive
            ? db.Products.IgnoreQueryFilters()
            : db.Products;

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.Weight,
                p.Length,
                p.Width,
                p.Height,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(cancellationToken);

        // Cache active products list
        if (!request.IncludeInactive)
        {
            await cache.SetStringAsync(
                CacheKey,
                JsonSerializer.Serialize(products),
                CacheOptions,
                cancellationToken);
        }

        return products;
    }
}

// === Get Product ===
public sealed record GetProductQuery(Guid Id) : IRequest<ProductDto?>;

public sealed class GetProductHandler(InventoryDbContext db, IDistributedCache cache)
    : IRequestHandler<GetProductQuery, ProductDto?>
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    public async Task<ProductDto?> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:{request.Id}";
        
        // Try cache first
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<ProductDto>(cached);
        }

        var product = await db.Products
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.Weight,
                p.Length,
                p.Width,
                p.Height,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (product is not null)
        {
            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(product),
                CacheOptions,
                cancellationToken);
        }

        return product;
    }
}

// === Create Product ===
public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height) : IRequest<CreateProductResult>;

public sealed record CreateProductResult(bool Success, Guid? Id = null, string? Error = null);

public sealed class CreateProductHandler(InventoryDbContext db, IDistributedCache cache)
    : IRequestHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate SKU
        var existingSku = await db.Products
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Sku == request.Sku.Trim().ToUpperInvariant(), cancellationToken);

        if (existingSku)
        {
            return new CreateProductResult(false, Error: $"Product with SKU '{request.Sku}' already exists.");
        }

        var productResult = Product.Create(
            request.Sku,
            request.Name,
            request.Description,
            request.Weight,
            request.Length,
            request.Width,
            request.Height);

        if (productResult.IsFailure)
        {
            return new CreateProductResult(false, Error: productResult.Error.Message);
        }

        db.Products.Add(productResult.Value);
        await db.SaveChangesAsync(cancellationToken);

        // Invalidate list cache
        await cache.RemoveAsync("products:list", cancellationToken);

        return new CreateProductResult(true, productResult.Value.Id);
    }
}

// === Update Product ===
public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height) : IRequest<UpdateProductResult>;

public sealed record UpdateProductResult(bool Success, string? Error = null);

public sealed class UpdateProductHandler(InventoryDbContext db, IDistributedCache cache)
    : IRequestHandler<UpdateProductCommand, UpdateProductResult>
{
    public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products.FindAsync([request.Id], cancellationToken);
        
        if (product is null)
        {
            return new UpdateProductResult(false, Error: "Product not found.");
        }

        var updateResult = product.Update(
            request.Name,
            request.Description,
            request.Weight,
            request.Length,
            request.Width,
            request.Height);

        if (updateResult.IsFailure)
        {
            return new UpdateProductResult(false, Error: updateResult.Error.Message);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Invalidate caches
        await cache.RemoveAsync("products:list", cancellationToken);
        await cache.RemoveAsync($"products:{request.Id}", cancellationToken);

        return new UpdateProductResult(true);
    }
}

// === Endpoints ===
public static class ProductEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        var products = group.MapGroup("/products").WithTags("Products");

        products.MapGet("/", async (IMediator mediator, bool includeInactive = false) =>
        {
            var result = await mediator.Send(new ListProductsQuery(includeInactive));
            return Results.Ok(result);
        })
        .WithName("ListProducts")
        .WithSummary("List all products");

        products.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetProductQuery(id));
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { error = "Product not found." });
        })
        .WithName("GetProduct")
        .WithSummary("Get a product by ID");

        products.MapPost("/", async (CreateProductCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.Success
                ? Results.Created($"/inventory/products/{result.Id}", new { id = result.Id })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CreateProduct")
        .WithSummary("Create a new product");

        products.MapPut("/{id:guid}", async (Guid id, UpdateProductCommand command, IMediator mediator) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest(new { error = "ID in URL does not match ID in body." });
            }

            var result = await mediator.Send(command);
            return result.Success
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("UpdateProduct")
        .WithSummary("Update an existing product");
    }
}
