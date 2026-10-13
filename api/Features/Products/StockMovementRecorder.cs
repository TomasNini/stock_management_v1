using StockManagement.Api.Infrastructure.Data.Entities;

namespace StockManagement.Api.Infrastructure.Data;

/// <summary>
/// Shared write path for the stock ledger. Delta is the change for this event
/// (+ in, − out), not the new total. Zero deltas are skipped.
/// </summary>
public static class StockMovementRecorder {
    public static void Add(MainDbContext dbContext, int productId, int delta, DateTimeOffset? createdAt = null) {
        if (delta == 0) {
            return;
        }

        dbContext.StockMovements.Add(new StockMovement {
            ProductId = productId,
            Delta = delta,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        });
    }
}
