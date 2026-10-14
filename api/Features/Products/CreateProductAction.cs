using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StockManagement.Api.Infrastructure.Data;
using StockManagement.Api.Infrastructure.Data.Entities;

namespace StockManagement.Api.Features.Products.ProductManager;

/// <summary>
/// POST /api/products
/// Creates a product and records an initial stock movement with delta = starting quantity.
/// </summary>
[ApiController]
[Route("products")]
public class CreateProductAction(MainDbContext dbContext) : ControllerBase {
    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] CreateProductRequestDTO request) {
        var now = DateTime.UtcNow;
        var entity = new Product {
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Quantity = request.Quantity,
            MinQuantity = request.MinQuantity,
            UnitPrice = request.UnitPrice,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync();
        StockMovementRecorder.Add(dbContext, entity.Id, entity.Quantity);
        await dbContext.SaveChangesAsync();

        return Ok(new CreateProductResponseDTO(
            entity.Id,
            entity.Name,
            entity.Sku,
            entity.Description,
            entity.Quantity,
            entity.MinQuantity,
            entity.UnitPrice,
            entity.CreatedAt,
            entity.UpdatedAt));
    }

    public class CreateProductRequestDTO {
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

    public record CreateProductResponseDTO(
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
