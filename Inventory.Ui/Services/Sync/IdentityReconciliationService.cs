using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Sync;
using Inventory.Ui.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Ui.Services.Sync;

public sealed class IdentityReconciliationService(PosLocalDbContext db, ILocalTenantContext tenant, IProductApi products)
{
    public async Task ReconcileAsync(CancellationToken ct)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var productCreates = await db.SyncQueueItems.AsNoTracking().AnyAsync(x => x.TenantId == tenantId &&
            x.EntityName == "Product" && x.Operation == SyncOperation.Create &&
            (x.Status == SyncQueueStatus.Conflict || x.Status == SyncQueueStatus.Pending || x.Status == SyncQueueStatus.Failed), ct);
        var serverProducts = productCreates ? await products.GetAll(ct) : [];
        if (tenant.TenantId != tenantId) throw new InvalidOperationException("Le magasin a changé pendant la synchronisation.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var creates = await db.SyncQueueItems.Where(x => x.TenantId == tenantId && x.EntityName == "Product" &&
            x.Operation == SyncOperation.Create && (x.Status == SyncQueueStatus.Conflict ||
            x.Status == SyncQueueStatus.Pending || x.Status == SyncQueueStatus.Failed)).ToListAsync(ct);
        foreach (var queue in creates)
        {
            var local = await db.Products.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == queue.LocalEntityId, ct);
            var matches = serverProducts.Where(x => x.CatalogProductId == local?.CatalogProductId).ToArray();
            if (matches.Length == 1)
                await ProductIdentityReconciler.ApplyAsync(db, tenantId, queue, matches[0], ct);
        }
        // Replay only lost customer acknowledgements with the ORIGINAL payload and key.
        // The server's idempotency record supplies the identifier; error text is not identity proof.
        var customerConflicts = await db.SyncQueueItems.Where(x => x.TenantId == tenantId &&
            x.EntityName == "Customer" && x.Operation == SyncOperation.Create && x.Status == SyncQueueStatus.Conflict &&
            x.ErrorMessage != null && x.ErrorMessage.StartsWith("Server Customer '") &&
            x.ErrorMessage.Contains("is already linked to local Customer")).ToListAsync(ct);
        foreach (var queue in customerConflicts)
        {
            queue.Status = SyncQueueStatus.Pending;
            queue.NextAttemptAtUtc = null;
            queue.BatchId = null;
            queue.LockedAtUtc = null;
        }
        var unexplainedStockConflicts = await db.SyncQueueItems.Where(x => x.TenantId == tenantId &&
            x.EntityName == "StockMovement" && x.Status == SyncQueueStatus.Conflict &&
            (x.ErrorMessage == null || x.ErrorMessage == "")).ToListAsync(ct);
        foreach (var queue in unexplainedStockConflicts)
            queue.ErrorMessage = "Ancien conflit de mouvement de stock : vérifier l'enregistrement serveur avant toute réémission pour éviter de compter le stock deux fois. Opération : " + queue.ClientOperationId;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
