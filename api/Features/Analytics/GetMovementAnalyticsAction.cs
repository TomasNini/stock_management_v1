using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Analytics;

/// <summary>
/// GET analytics/get-movement-analytics
/// Recent stock movements for Analytics. Optional query: period=7|30|all (default 30)
/// and productId to scope one product.
/// </summary>
[ApiController]
[Route("analytics/get-movement-analytics")]
public class GetMovementAnalyticsAction(MainDbContext dbContext) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Handle([FromQuery] string period = "30", [FromQuery] int? productId = null) {
        var movementsQuery = dbContext.StockMovements.AsNoTracking();
        if (productId is not null) {
            movementsQuery = movementsQuery.Where(x => x.ProductId == productId);
        }

        var movements = await movementsQuery
            .Select(x => new { ProductName = x.Product.Name, x.Delta, x.CreatedAt })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = ResolveFrom(period, today);

        // Newest first; the frontend slices this list for the pager.
        var recent = movements
            .Where(x => {
                if (from is null) {
                    return true;
                }

                var day = DateOnly.FromDateTime(x.CreatedAt.UtcDateTime);
                return day >= from && day <= today;
            })
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RecentMovementResponseDTO(x.CreatedAt, x.ProductName, x.Delta))
            .ToList();

        return Ok(new GetMovementAnalyticsResponseDTO(recent));
    }

    /// <summary>
    /// Inclusive start date for 7 or 30 days. Null means the full ledger (period=all).
    /// </summary>
    private static DateOnly? ResolveFrom(string period, DateOnly today) {
        if (string.Equals(period, "7", StringComparison.OrdinalIgnoreCase)) {
            return today.AddDays(-6);
        }

        if (string.Equals(period, "all", StringComparison.OrdinalIgnoreCase)) {
            return null;
        }

        return today.AddDays(-29);
    }

    public record GetMovementAnalyticsResponseDTO(IReadOnlyList<RecentMovementResponseDTO> Recent);

    public record RecentMovementResponseDTO(DateTimeOffset CreatedAt, string ProductName, int Delta);
}
