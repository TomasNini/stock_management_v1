using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data.Entities;

namespace StockManagement.Api.Infrastructure.Data;

public static class SeedData {
    //#region Constants

    private const int TargetProductCount = 200;
    private const int TargetMovementCount = 500;

    private static readonly string[] Adjectives = [
        "Oak", "Steel", "Compact", "Heavy", "Portable", "Industrial", "Office", "Warehouse",
        "Safety", "Packing", "Wireless", "USB", "Large", "Small", "Blue", "Black", "Orange",
        "Folding", "Magnetic", "Recycled", "Premium", "Budget", "Silent", "Ergonomic", "Rugged"];

    private static readonly string[] Nouns = [
        "Chair", "Lamp", "Mouse", "Cable", "Notebook", "Tape", "Gloves", "Scanner", "Bracket",
        "Bin", "Monitor", "Keyboard", "Headset", "Adapter", "Stapler", "Pallet", "Crate",
        "Helmet", "Vest", "Hammer", "Screwdriver", "Wrench", "Label", "Ribbon", "Battery",
        "Charger", "Shelf", "Cart", "Scale", "Cutter"];

    //#endregion

    public static async Task Apply(MainDbContext dbContext) {
        if (!await dbContext.Settings.AnyAsync()) {
            dbContext.Settings.AddRange(
                new AppSetting { Setting = "app.name", Value = "Stock management" },
                new AppSetting { Setting = "app.currency", Value = "EUR" },
                new AppSetting { Setting = "color.menu-background", Value = "#1f2937" },
                new AppSetting { Setting = "color.menu-text", Value = "#f9fafb" },
                new AppSetting { Setting = "color.menu-active", Value = "#2563eb" },
                new AppSetting { Setting = "color.button", Value = "#2563eb" },
                new AppSetting { Setting = "color.text", Value = "#111827" },
                new AppSetting { Setting = "color.page-background", Value = "#f3f4f6" });
        }

        await EnsureProducts(dbContext);
        await EnsureMovements(dbContext);
        await dbContext.SaveChangesAsync();
    }

    //#region Products

    private static async Task EnsureProducts(MainDbContext dbContext) {
        var missing = TargetProductCount - await dbContext.Products.CountAsync();
        if (missing <= 0) {
            return;
        }

        var usedNames = new HashSet<string>(await dbContext.Products.Select(x => x.Name).ToListAsync(), StringComparer.OrdinalIgnoreCase);
        var usedSkus = new HashSet<string>(await dbContext.Products.Select(x => x.Sku).ToListAsync(), StringComparer.OrdinalIgnoreCase);
        var skuNumber = 1;
        var now = DateTimeOffset.UtcNow;
        var products = new List<Product>();

        foreach (var adjective in Adjectives) {
            foreach (var noun in Nouns) {
                if (products.Count >= missing) {
                    break;
                }

                var name = $"{adjective} {noun}";
                if (!usedNames.Add(name)) {
                    continue;
                }

                var createdAt = DayAt(now, 20 + products.Count % 160, 8, products.Count);
                products.Add(new Product {
                    Name = name,
                    Sku = NextSku(usedSkus, ref skuNumber),
                    Description = $"{name} for warehouse and office use.",
                    Quantity = 0,
                    MinQuantity = 4 + products.Count % 16,
                    UnitPrice = 1.25m + (products.Count * 3.4m % 180),
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                });
            }

            if (products.Count >= missing) {
                break;
            }
        }

        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync();
    }

    private static string NextSku(HashSet<string> usedSkus, ref int skuNumber) {
        while (true) {
            var sku = $"PRD-{skuNumber:D4}";
            skuNumber++;
            if (usedSkus.Add(sku)) {
                return sku;
            }
        }
    }

    //#endregion

    //#region Movements

    private static async Task EnsureMovements(MainDbContext dbContext) {
        var products = await dbContext.Products.ToListAsync();
        var movements = await dbContext.StockMovements
            .Select(x => new { x.ProductId, x.Delta, x.CreatedAt })
            .ToListAsync();

        var usedTimes = new HashSet<DateTimeOffset>(movements.Select(x => x.CreatedAt));
        var runningQty = products.ToDictionary(x => x.Id, x => x.Quantity);
        var lastAt = products.ToDictionary(x => x.Id, x => x.CreatedAt);
        foreach (var movement in movements) {
            lastAt[movement.ProductId] = movement.CreatedAt > lastAt.GetValueOrDefault(movement.ProductId)
                ? movement.CreatedAt
                : lastAt.GetValueOrDefault(movement.ProductId);
        }

        var productsWithoutLedger = products
            .Where(x => movements.All(movement => movement.ProductId != x.Id))
            .ToList();

        var added = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var product in productsWithoutLedger) {
            var opening = 6 + product.Id % 40;
            if (product.Id % 17 == 0) {
                opening = 0;
            }

            if (opening > 0) {
                AddMovement(dbContext, usedTimes, runningQty, lastAt, product, opening, product.CreatedAt);
                added++;
            }
        }

        var rng = new Random(20261011);
        var needed = TargetMovementCount - movements.Count - added;
        var guard = 0;
        while (needed > 0 && guard < TargetMovementCount * 8) {
            guard++;
            var product = products[rng.Next(products.Count)];
            var current = runningQty[product.Id];
            var delta = rng.Next(0, 5) == 0 ? -rng.Next(1, 9) : rng.Next(1, 14);
            if (current + delta < 0) {
                if (current == 0) {
                    delta = rng.Next(2, 16);
                } else {
                    delta = -current;
                }
            }

            if (delta == 0) {
                continue;
            }

            var daysAgo = rng.Next(0, 150);
            var createdAt = DayAt(now, daysAgo, rng.Next(7, 19), rng.Next(0, 60));
            if (createdAt < product.CreatedAt) {
                createdAt = product.CreatedAt.AddHours(1);
            }

            AddMovement(dbContext, usedTimes, runningQty, lastAt, product, delta, createdAt);
            needed--;
            added++;
        }

        foreach (var product in products) {
            product.Quantity = runningQty[product.Id];
            product.UpdatedAt = lastAt[product.Id];
        }
    }

    private static void AddMovement(
        MainDbContext dbContext,
        HashSet<DateTimeOffset> usedTimes,
        Dictionary<int, int> runningQty,
        Dictionary<int, DateTimeOffset> lastAt,
        Product product,
        int delta,
        DateTimeOffset createdAt) {
        createdAt = UniqueTime(usedTimes, createdAt);
        StockMovementRecorder.Add(dbContext, product.Id, delta, createdAt);
        runningQty[product.Id] = runningQty.GetValueOrDefault(product.Id) + delta;
        if (createdAt > lastAt.GetValueOrDefault(product.Id)) {
            lastAt[product.Id] = createdAt;
        }
    }

    //#endregion

    //#region Dates

    private static DateTimeOffset DayAt(DateTimeOffset now, int daysAgo, int hour, int minuteSalt) {
        var day = now.UtcDateTime.Date.AddDays(-daysAgo).AddHours(hour).AddMinutes(minuteSalt % 60);
        return new DateTimeOffset(day, TimeSpan.Zero);
    }

    private static DateTimeOffset UniqueTime(HashSet<DateTimeOffset> usedTimes, DateTimeOffset value) {
        while (!usedTimes.Add(value)) {
            value = value.AddMinutes(1);
        }

        return value;
    }

    //#endregion
}
