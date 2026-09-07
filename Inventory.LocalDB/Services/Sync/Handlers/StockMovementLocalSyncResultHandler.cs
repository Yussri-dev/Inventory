using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services.Sync.Handlers;

public sealed class StockMovementLocalSyncResultHandler
    : ILocalSyncResultHandler
{
    private const string StockMovementEntityName =
        "StockMovement";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public StockMovementLocalSyncResultHandler(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public string EntityName =>
        StockMovementEntityName;

    public async Task ApplyAsync(
        SyncQueueItem queueItem,
        SyncBatchItemResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            queueItem);

        ArgumentNullException.ThrowIfNull(
            result);

        if (!result.IsSuccessful)
        {
            return;
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        if (queueItem.TenantId != tenantId)
        {
            throw new InvalidOperationException(
                "The StockMovement synchronization result " +
                "belongs to another tenant.");
        }

        var movement =
            await _db.StockMovements
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id ==
                            queueItem.LocalEntityId,
                    cancellationToken);

        if (movement == null)
        {
            throw new InvalidOperationException(
                $"Local StockMovement " +
                $"'{queueItem.LocalEntityId}' was not found.");
        }

        var serverEntityId =
            result.ServerEntityId ??
            queueItem.ServerEntityId ??
            movement.ServerId;

        if (!serverEntityId.HasValue ||
            serverEntityId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The server returned no StockMovement identifier.");
        }

        if (movement.ServerId.HasValue &&
            movement.ServerId.Value != Guid.Empty &&
            movement.ServerId.Value != serverEntityId.Value)
        {
            result.Status =
                SyncBatchItemStatus.Conflict;

            result.ErrorMessage =
                $"Local StockMovement '{movement.Id}' is already " +
                "linked to a different server identifier.";

            return;
        }

        var trackedOwner =
            _db.ChangeTracker
                .Entries<LocalStockMovement>()
                .Select(entry =>
                    entry.Entity)
                .FirstOrDefault(item =>
                    item.TenantId == tenantId &&
                    item.Id != movement.Id &&
                    item.ServerId ==
                        serverEntityId.Value);

        var existingOwner =
            trackedOwner ??
            await _db.StockMovements
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id != movement.Id &&
                        item.ServerId ==
                            serverEntityId.Value,
                    cancellationToken);

        if (existingOwner != null)
        {
            result.Status =
                SyncBatchItemStatus.Conflict;

            result.ErrorMessage =
                $"Server StockMovement '{serverEntityId.Value}' " +
                $"is already linked to local StockMovement " +
                $"'{existingOwner.Id}'.";

            return;
        }

        movement.ServerId =
            serverEntityId.Value;

        movement.SyncStatus =
            SyncQueueStatus.Done;

        movement.LastSyncedAtUtc =
            DateTime.UtcNow;

    }
}