using Inventory.Dto.Enums;
using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services.Sync.Handlers;

public sealed class DamageLocalSyncResultHandler
    : ILocalSyncResultHandler
{
    private const string DamageEntityName =
        "Damage";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public DamageLocalSyncResultHandler(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public string EntityName =>
        DamageEntityName;

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
                "The Damage synchronization result belongs " +
                "to another tenant.");
        }

        var damage =
            await _db.Damages
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id ==
                            queueItem.LocalEntityId,
                    cancellationToken);

        if (damage == null)
        {
            throw new InvalidOperationException(
                $"Local Damage '{queueItem.LocalEntityId}' " +
                "was not found.");
        }

        var serverEntityId =
            result.ServerEntityId ??
            queueItem.ServerEntityId ??
            damage.ServerId;

        if (!serverEntityId.HasValue ||
            serverEntityId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The server returned no Damage identifier.");
        }

        if (damage.ServerId.HasValue &&
            damage.ServerId.Value != Guid.Empty &&
            damage.ServerId.Value != serverEntityId.Value)
        {
            result.Status =
                SyncBatchItemStatus.Conflict;

            result.ErrorMessage =
                $"Local Damage '{damage.Id}' is already linked " +
                "to a different server identifier.";

            return;
        }

        var trackedOwner =
            _db.ChangeTracker
                .Entries<LocalDamage>()
                .Select(entry =>
                    entry.Entity)
                .FirstOrDefault(item =>
                    item.TenantId == tenantId &&
                    item.Id != damage.Id &&
                    item.ServerId ==
                        serverEntityId.Value);

        var existingOwner =
            trackedOwner ??
            await _db.Damages
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id != damage.Id &&
                        item.ServerId ==
                            serverEntityId.Value,
                    cancellationToken);

        if (existingOwner != null)
        {
            result.Status =
                SyncBatchItemStatus.Conflict;

            result.ErrorMessage =
                $"Server Damage '{serverEntityId.Value}' is " +
                $"already linked to local Damage " +
                $"'{existingOwner.Id}'.";

            return;
        }

        /*
         * The local stock movement belongs to the Damage aggregate.
         * It was created during ValidateAllDraftsAsync and was not
         * queued separately.
         */
        var movements =
            await _db.StockMovements
                .Where(movement =>
                    movement.TenantId == tenantId &&
                    movement.LocalReferenceId == damage.Id &&
                    movement.ClientOperationId ==
                        queueItem.ClientOperationId &&
                    movement.Type ==
                        LocalStockMovementType.Damage)
                .ToListAsync(
                    cancellationToken);

        if (movements.Count != 1)
        {
            result.Status =
                SyncBatchItemStatus.Conflict;

            result.ErrorMessage =
                movements.Count == 0
                    ? $"No local stock movement was found for " +
                      $"Damage '{damage.Id}'."
                    : $"Multiple local stock movements were found " +
                      $"for Damage '{damage.Id}'.";

            return;
        }

        var now =
            DateTime.UtcNow;

        damage.ServerId =
            serverEntityId.Value;

        damage.LocalStatus =
            LocalDamageStatus.Synced;

        damage.ServerStatus =
            DamageStatus.Validated.ToString();

        damage.SyncStatus =
            SyncQueueStatus.Done;

        damage.LastSyncedAtUtc =
            now;

        var movement =
            movements[0];

        /*
         * ServerReferenceId identifies the parent server Damage.
         * movement.ServerId remains null because the batch result
         * returns the Damage identifier, not the child movement id.
         */
        movement.ServerReferenceId =
            serverEntityId.Value;

        movement.SyncStatus =
            SyncQueueStatus.Done;

        movement.LastSyncedAtUtc =
            now;

        /*
         * Do not modify LocalStock or LocalProduct here.
         * Their quantities were already reduced atomically when
         * the Damage drafts were validated.
         *
         * SyncQueueService owns SaveChangesAsync and the local
         * transaction.
         */
    }
}