using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Products.ProductManager;

/// <summary>
/// POST /api/products/{id}/add-to-stock/{quantity}
/// Increases stock and records a movement with a positive delta.
/// </summary>
[ApiController]
[Route("products/{id:int}/add-to-stock/{quantity:int}")]
public class AddToStockAction(MainDbContext dbContext) : ControllerBase {
    [HttpPost]
    public async Task<IActionResult> Handle(int id, int quantity) {
        if (quantity <= 0) {
            return BadRequest("Quantity must be greater than 0");
        }

        var entity = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) {
            return NotFound();
        }

        entity.Quantity += quantity;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        StockMovementRecorder.Add(dbContext, entity.Id, quantity);
        await dbContext.SaveChangesAsync();

        return Ok();
    }
}
