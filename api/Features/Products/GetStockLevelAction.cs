using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Products.ProductList;

/// <summary>
/// GET /api/products/stock-level
/// Products whose current Quantity is between min and max (inclusive). Defaults: min=0, max=int.MaxValue.
/// </summary>
[ApiController]
[Route("products/stock-level")]
public class GetStockLevelAction(MainDbContext dbContext) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Handle([FromQuery] int min = 0, [FromQuery] int max = int.MaxValue) {
        if (min < 0 || max < min) {
            return BadRequest("Stock range is invalid");
        }

        var items = await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Quantity >= min && x.Quantity <= max)
            .OrderBy(x => x.Quantity)
            .Select(x => new GetStockLevelResponseDTO(
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

    public record GetStockLevelResponseDTO(
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
