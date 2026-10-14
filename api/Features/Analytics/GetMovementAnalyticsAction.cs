using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Features.Analytics;

/// <summary>
/// GET analytics/get-movement-analytics
/// One page of stock movements, newest first. Query: period=7|30|all (default 30),
/// optional productId, page (default 1) and pageSize (default 10, max 100).
/// </summary>
[ApiController]
[Route("analytics/get-movement-analytics")]
public class GetMovementAnalyticsAction(MainDbContext dbContext) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Handle(
        [FromQuery] string period = "30",
        [FromQuery] int? productId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10) {
        if (page < 1) {
            return BadRequest("Page must be at least 1");
        }

        if (pageSize < 1 || pageSize > 100) {
            return BadRequest("Page size must be between 1 and 100");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = dbContext.StockMovements.AsNoTracking();
        if (productId is not null) {
            query = query.Where(x => x.ProductId == productId);
        }

        var from = PeriodStart(period, today);
        if (from is DateTime start) {
            var until = StartOfUtcDay(today.AddDays(1));
            query = query.Where(x => x.CreatedAt >= start && x.CreatedAt < until);
        }

        var total = await query.CountAsync();
        var recent = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RecentMovementResponseDTO(x.CreatedAt, x.Product.Name, x.Delta))
            .ToListAsync();

        return Ok(new GetMovementAnalyticsResponseDTO(recent, total));
    }

    /// <summary>
    /// Inclusive UTC start for 7 or 30 days. Null means the full ledger (period=all).
    /// </summary>
    private static DateTime? PeriodStart(string period, DateOnly today) {
        if (string.Equals(period, "7", StringComparison.OrdinalIgnoreCase)) {
            return StartOfUtcDay(today.AddDays(-6));
        }

        if (string.Equals(period, "all", StringComparison.OrdinalIgnoreCase)) {
            return null;
        }

        return StartOfUtcDay(today.AddDays(-29));
    }

    private static DateTime StartOfUtcDay(DateOnly day) {
        return day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    public record GetMovementAnalyticsResponseDTO(IReadOnlyList<RecentMovementResponseDTO> Recent, int Total);

    public record RecentMovementResponseDTO(DateTime CreatedAt, string ProductName, int Delta);
}
