using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services;

public static class PendingStockProtection
{
    public static async Task<HashSet<Guid>> GetProductIdsAsync(PosLocalDbContext db, Guid tenantId, CancellationToken ct)
    {
        // Pack sales are represented by movements on their component products.
        var ids = await db.StockMovements.AsNoTracking()
            .Where(m => m.TenantId == tenantId && db.SyncQueueItems.Any(q =>
                q.TenantId == tenantId && q.Status != SyncQueueStatus.Done && q.Status != SyncQueueStatus.Draft &&
                (q.LocalEntityId == m.Id || (m.LocalReferenceId.HasValue && q.LocalEntityId == m.LocalReferenceId.Value))))
            .Select(m => m.ProductLocalId).Distinct().ToListAsync(ct);
        return ids.ToHashSet();
    }
}
