using AspireWms.Api.Modules.Inventory.Domain.Entities;
using AspireWms.Api.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AspireWms.Api.Modules.Inventory.Infrastructure;

public static class InventoryDbSeeder
{
    public static async Task SeedAsync(InventoryDbContext context)
    {
        // Only seed if database is empty
        if (await context.Products.IgnoreQueryFilters().AnyAsync())
            return;

        // === Seed Products ===
        var products = new List<Product>();
        var productData = new[]
        {
            ("SKU001", "Wireless Mouse", "Ergonomic wireless mouse with USB receiver", 0.15m, 12.0m, 7.0m, 4.0m),
            ("SKU002", "Mechanical Keyboard", "RGB mechanical keyboard with Cherry MX switches", 1.2m, 45.0m, 15.0m, 3.5m),
            ("SKU003", "USB-C Hub", "7-port USB-C hub with HDMI and ethernet", 0.25m, 15.0m, 8.0m, 2.0m),
            ("SKU004", "Monitor Stand", "Adjustable monitor stand with USB ports", 2.5m, 40.0m, 25.0m, 15.0m),
            ("SKU005", "Webcam HD", "1080p HD webcam with built-in microphone", 0.18m, 10.0m, 8.0m, 7.0m),
            ("SKU006", "Laptop Sleeve", "Neoprene laptop sleeve 15 inch", 0.3m, 40.0m, 30.0m, 2.0m),
            ("SKU007", "USB Flash Drive 64GB", "High-speed USB 3.0 flash drive", 0.02m, 6.0m, 2.0m, 1.0m),
            ("SKU008", "Ethernet Cable 2m", "Cat6 ethernet cable, blue", 0.1m, 200.0m, 1.0m, 1.0m),
            ("SKU009", "Power Bank 10000mAh", "Portable power bank with dual USB", 0.35m, 14.0m, 7.0m, 2.5m),
            ("SKU010", "Wireless Earbuds", "Bluetooth 5.0 wireless earbuds with charging case", 0.05m, 6.0m, 6.0m, 3.0m),
        };

        foreach (var (sku, name, desc, weight, length, width, height) in productData)
        {
            var result = Product.Create(sku, name, desc, weight, length, width, height);
            if (result.IsSuccess)
                products.Add(result.Value);
        }

        context.Products.AddRange(products);

        // === Seed Locations ===
        var locations = new List<Location>();
        
        // Zone A: Receiving (10 locations)
        for (int aisle = 1; aisle <= 2; aisle++)
            for (int rack = 1; rack <= 2; rack++)
                for (int bin = 1; bin <= 2; bin++)
                {
                    var result = Location.Create("A", aisle, rack, bin, capacity: 50);
                    if (result.IsSuccess)
                        locations.Add(result.Value);
                }

        // Zone B: Storage (20 locations)
        for (int aisle = 1; aisle <= 4; aisle++)
            for (int rack = 1; rack <= 2; rack++)
                for (int bin = 1; bin <= 2; bin++)
                {
                    var result = Location.Create("B", aisle, rack, bin, capacity: 100);
                    if (result.IsSuccess)
                        locations.Add(result.Value);
                }

        // Zone C: Shipping (10 locations)
        for (int aisle = 1; aisle <= 2; aisle++)
            for (int rack = 1; rack <= 2; rack++)
                for (int bin = 1; bin <= 2; bin++)
                {
                    var result = Location.Create("C", aisle, rack, bin, capacity: 75);
                    if (result.IsSuccess)
                        locations.Add(result.Value);
                }

        context.Locations.AddRange(locations);
        await context.SaveChangesAsync();

        // === Seed Inventory Items ===
        var random = new Random(42); // Seed for reproducible results
        var inventoryItems = new List<InventoryItem>();

        // Distribute products across storage zone B
        var storageLocations = locations.Where(l => l.Zone == "B").ToList();
        
        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var location = storageLocations[i % storageLocations.Count];
            var quantity = Quantity.Create(random.Next(10, 100)).Value;

            var itemResult = InventoryItem.Create(
                product.Id,
                location.Id,
                quantity,
                "Initial seed data");

            if (itemResult.IsSuccess)
                inventoryItems.Add(itemResult.Value);
        }

        context.InventoryItems.AddRange(inventoryItems);
        await context.SaveChangesAsync();
    }
}
