using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Products.ProductManager;

/// <summary>
/// POST /api/products/{id}/decrement-stock/{quantity}
/// Decreases stock (rejects if there is not enough) and records a movement with a negative delta.
/// </summary>
[ApiController]
[Route("products/{id:int}/decrement-stock/{quantity:int}")]
public class DecrementStockAction(MainDbContext dbContext) : ControllerBase {
    [HttpPost]
    public async Task<IActionResult> Handle(int id, int quantity) {
        if (quantity <= 0) {
            return BadRequest("Quantity must be greater than 0");
        }

        var entity = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) {
            return NotFound();
        }

        if (entity.Quantity < quantity) {
            return BadRequest("Insufficient stock");
        }

        entity.Quantity -= quantity;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        StockMovementRecorder.Add(dbContext, entity.Id, -quantity);
        await dbContext.SaveChangesAsync();

        return Ok();
    }
}
