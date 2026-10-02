using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.LocalDB.Services.Sync;

public static class ProductIdentityReconciler
{
    // 'server' must come from the authenticated tenant's product endpoint.
    public static async Task<bool> ApplyAsync(PosLocalDbContext db, Guid tenantId,
        SyncQueueItem queue, ProductResult server, CancellationToken ct)
    {
        if (queue.TenantId != tenantId || queue.EntityName != "Product" || queue.Operation != SyncOperation.Create ||
            queue.Status == SyncQueueStatus.Done || queue.Status == SyncQueueStatus.Processing || server.Id == Guid.Empty)
            return false;
        var local = await db.Products.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == queue.LocalEntityId, ct);
        if (local == null || local.IsDeletedLocally || local.CatalogProductId != server.CatalogProductId ||
            (local.ServerId.HasValue && local.ServerId != server.Id) ||
            local.IsPack != server.IsPack || (local.IsPack && (local.UnitsPerPack != server.PackSize || local.UnitProductServerId != server.ComponentProductId)))
            return false;
        if (await db.Products.AnyAsync(x => x.TenantId == tenantId && x.Id != local.Id && x.ServerId == server.Id, ct) ||
            await db.SyncQueueItems.AnyAsync(x => x.TenantId == tenantId && x.EntityName == "Product" &&
                x.LocalEntityId == local.Id && x.Id != queue.Id && x.Status != SyncQueueStatus.Done, ct)) return false;

        var changed = local.SalePrice != server.SalePrice || local.SalePrice2 != server.SalePrice2 ||
            local.SalePrice3 != server.SalePrice3 || local.PurchasePrice != server.PurchasePrice ||
            local.VatRate != server.VatRate || local.MinStockLevel != server.MinStockLevel ||
            local.MaxStockLevel != server.MaxStockLevel || local.IsTracked != server.IsTracked || local.Status != server.Status;
        local.ServerId = server.Id;
        local.SyncStatus = changed ? SyncQueueStatus.Pending : SyncQueueStatus.Done;
        if (!changed) local.LastSyncedAtUtc = DateTime.UtcNow;
        queue.ServerEntityId = server.Id;
        queue.Status = SyncQueueStatus.Done;
        queue.ProcessedAtUtc = DateTime.UtcNow;
        queue.ErrorMessage = null;
        queue.NextAttemptAtUtc = null;
        if (changed)
        {
            // New semantic operation => new idempotency key. Keep the old Create for audit.
            db.SyncQueueItems.Add(new SyncQueueItem
            {
                TenantId = tenantId, EntityName = "Product", LocalEntityId = local.Id,
                ServerEntityId = server.Id, Operation = SyncOperation.Update,
                PayloadJson = JsonSerializer.Serialize(new UpdateProductRequest
                {
                    SalePrice = local.SalePrice, SalePrice2 = local.SalePrice2, SalePrice3 = local.SalePrice3,
                    PurchasePrice = local.PurchasePrice, VatRate = local.VatRate,
                    MinStockLevel = local.MinStockLevel, MaxStockLevel = local.MaxStockLevel,
                    IsTracked = local.IsTracked, IsActive = local.Status
                })
            });
        }
        return true;
    }
}
