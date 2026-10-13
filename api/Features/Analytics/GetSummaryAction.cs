using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Analytics;

/// <summary>
/// GET analytics/get-summary
/// Snapshot KPIs from current product stock: total value (qty * unitPrice), total units,
/// and the low-stock list (quantity 0, or quantity at/below MinQuantity when MinQuantity &gt; 0).
/// Does not use the movement ledger. Aggregates and the low-stock filter run in SQL.
/// </summary>
[ApiController]
[Route("analytics/get-summary")]
public class GetSummaryAction(MainDbContext dbContext) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Handle() {
        var products = dbContext.Products.AsNoTracking();

        var totalStockValue = await products.SumAsync(x => x.Quantity * x.UnitPrice);
        var totalUnits = await products.SumAsync(x => x.Quantity);
        var lowStock = await products
            .Where(x => x.Quantity == 0 || (x.MinQuantity > 0 && x.Quantity <= x.MinQuantity))
            .Select(x => new LowStockProductResponseDTO(
                x.Id,
                x.Name,
                x.Sku,
                x.Quantity,
                x.MinQuantity,
                x.UnitPrice))
            .ToListAsync();

        return Ok(new GetSummaryResponseDTO(totalStockValue, totalUnits, lowStock));
    }

    public record GetSummaryResponseDTO(
        decimal TotalStockValue,
        int TotalUnits,
        IReadOnlyList<LowStockProductResponseDTO> LowStock);

    public record LowStockProductResponseDTO(
        int Id,
        string Name,
        string Sku,
        int Quantity,
        int MinQuantity,
        decimal UnitPrice);
}
