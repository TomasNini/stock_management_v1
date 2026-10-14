using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Products.ProductManager;

/// <summary>
/// PUT /api/products/{id}
/// Updates product fields. If quantity changed, writes a stock movement with delta = new − old.
/// </summary>
[ApiController]
[Route("products/{id:int}")]
public class EditProductAction(MainDbContext dbContext) : ControllerBase {
    [HttpPut]
    public async Task<IActionResult> Handle(int id, [FromBody] EditProductRequestDTO request) {
        var entity = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) {
            return NotFound();
        }

        // Delta is the change in quantity. 0 is ignored by StockMovementRecorder
        var quantityDelta = request.Quantity - entity.Quantity;
        entity.Name = request.Name.Trim();
        entity.Sku = request.Sku.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.Quantity = request.Quantity;
        entity.MinQuantity = request.MinQuantity;
        entity.UnitPrice = request.UnitPrice;
        entity.UpdatedAt = DateTime.UtcNow;
        StockMovementRecorder.Add(dbContext, entity.Id, quantityDelta);

        await dbContext.SaveChangesAsync();

        return Ok();
    }

    public class EditProductRequestDTO {
        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Sku { get; set; } = "";

        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Min quantity cannot be negative")]
        public int MinQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative")]
        public decimal UnitPrice { get; set; }
    }
}
