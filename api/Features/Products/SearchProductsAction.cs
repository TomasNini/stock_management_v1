using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Products.ProductList;

/// <summary>
/// GET /api/products/search?name=
/// Finds products whose name contains the term. Name is required; empty name returns no rows.
/// </summary>
[ApiController]
[Route("products/search")]
public class SearchProductsAction(MainDbContext dbContext) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Handle([FromQuery] string name = "") {
        var term = name.Trim();
        if (term.Length == 0) {
            return Ok(Array.Empty<SearchProductsResponseDTO>());
        }

        var pattern = $"%{EscapeLike(term)}%";
        var items = await dbContext.Products
            .AsNoTracking()
            .Where(x => EF.Functions.Like(x.Name, pattern, "\\"))
            .OrderBy(x => x.Name)
            .Select(x => new SearchProductsResponseDTO(
                x.Id,
                x.Name,
                x.Sku,
                x.Description,
                x.Quantity,
                x.MinQuantity,
                x.UnitPrice,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// SQLite has no ILIKE. LIKE is case-insensitive for ASCII; escape % and _ so they are literal.
    /// </summary>
    private static string EscapeLike(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    public record SearchProductsResponseDTO(
        int Id,
        string Name,
        string Sku,
        string? Description,
        int Quantity,
        int MinQuantity,
        decimal UnitPrice,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
