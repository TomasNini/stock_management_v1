using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Products.ProductList;

/// <summary>
/// GET /api/products/{id}
/// Loads a single product for the edit dialog.
/// </summary>
[ApiController]
[Route("products/{id:int}")]
public class GetProductAction(MainDbContext dbContext) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Handle(int id) {
        var item = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is null) {
            return NotFound();
        }

        return Ok(new GetProductResponseDTO(
            item.Id,
            item.Name,
            item.Sku,
            item.Description,
            item.Quantity,
            item.MinQuantity,
            item.UnitPrice));
    }

    public record GetProductResponseDTO(
        int Id,
        string Name,
        string Sku,
        string? Description,
        int Quantity,
        int MinQuantity,
        decimal UnitPrice);
}
