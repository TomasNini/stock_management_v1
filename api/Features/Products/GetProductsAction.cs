using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Products.ProductList;

/// <summary>
/// GET /api/products
/// Full product catalog, ordered by name. Name search is GET /api/products/search.
/// </summary>
[ApiController]
[Route("products")]
public class GetProductsAction(MainDbContext dbContext) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Handle() {
        var items = await dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new GetProductsItemResponseDTO(
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

    public record GetProductsItemResponseDTO(
        int Id,
        string Name,
        string Sku,
        string? Description,
        int Quantity,
        int MinQuantity,
        decimal UnitPrice,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
