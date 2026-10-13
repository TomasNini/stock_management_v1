using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Products.ProductManager;

/// <summary>
/// DELETE /api/products/{id}
/// Deletes a product. Related stock movements are removed with it (cascade).
/// </summary>
[ApiController]
[Route("products/{id:int}")]
public class DeleteProductAction(MainDbContext dbContext) : ControllerBase {
    [HttpDelete]
    public async Task<IActionResult> Handle(int id) {
        var entity = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) {
            return NotFound();
        }
        
        dbContext.Products.Remove(entity);
        await dbContext.SaveChangesAsync();

        return Ok();
    }
}
