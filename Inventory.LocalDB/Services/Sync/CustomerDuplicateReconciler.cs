using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services.Sync;

public static class CustomerDuplicateReconciler
{
    // Only called after the server acknowledges this exact creation (ClientOperationId).
    // Archive an unused duplicate; never merge money or rewrite transaction history.
    public static async Task<bool> TryArchiveAsync(PosLocalDbContext db, SyncQueueItem queue,
        LocalCustomer duplicate, LocalCustomer owner, CancellationToken ct)
    {
        if (queue.Operation != SyncOperation.Create || queue.TenantId != duplicate.TenantId ||
            owner.TenantId != duplicate.TenantId || owner.Id == duplicate.Id ||
            !owner.ServerId.HasValue || owner.ServerId == Guid.Empty || duplicate.ServerId.HasValue ||
            duplicate.IsDeleted || owner.IsDeleted || duplicate.CurrentBalance != 0 ||
            duplicate.ModifiedAtUtc.HasValue || owner.SyncStatus != SyncQueueStatus.Done ||
            duplicate.Name != owner.Name || duplicate.Email != owner.Email || duplicate.Phone != owner.Phone ||
            duplicate.Address != owner.Address || duplicate.TaxNumber != owner.TaxNumber ||
            duplicate.Notes != owner.Notes || duplicate.CreditLimit != owner.CreditLimit ||
            duplicate.AllowCredit != owner.AllowCredit || duplicate.HasUnlimitedCredit != owner.HasUnlimitedCredit ||
            duplicate.IsActive != owner.IsActive) return false;

        var tenant = duplicate.TenantId;
        var id = duplicate.Id;
        if (await db.Sales.AnyAsync(x => x.TenantId == tenant && x.CustomerLocalId == id, ct) ||
            await db.CustomerTransactions.AnyAsync(x => x.TenantId == tenant && x.CustomerLocalId == id, ct) ||
            await db.Returns.AnyAsync(x => x.TenantId == tenant && x.CustomerLocalId == id, ct) ||
            await db.SyncQueueItems.AnyAsync(x => x.TenantId == tenant && x.Id != queue.Id &&
                x.EntityName == "Customer" && (x.LocalEntityId == id || x.LocalEntityId == owner.Id) &&
                x.Status != SyncQueueStatus.Done, ct)) return false;

        duplicate.IsDeleted = true;
        duplicate.IsActive = false;
        duplicate.DeletedAtUtc = DateTime.UtcNow;
        duplicate.LastSyncedAtUtc = DateTime.UtcNow;
        duplicate.SyncStatus = SyncQueueStatus.Done;
        // The original queue entry retains the source ID and acknowledged server ID for audit.
        queue.ServerEntityId = owner.ServerId;
        return true;
    }
}
