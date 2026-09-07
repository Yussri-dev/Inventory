using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services.Sync
{
    public sealed class SyncQueueService
        : ISyncQueueService
    {
        private const int MaximumBatchSize =
            5_000;

        private const int MaximumErrorLength =
            2_000;

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        private readonly IReadOnlyDictionary<
            string,
            ILocalSyncResultHandler> _resultHandlers;

        public SyncQueueService(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext,
            IEnumerable<ILocalSyncResultHandler> resultHandlers)
        {
            ArgumentNullException.ThrowIfNull(
                db);

            ArgumentNullException.ThrowIfNull(
                tenantContext);

            ArgumentNullException.ThrowIfNull(
                resultHandlers);

            _db =
                db;

            _tenantContext =
                tenantContext;

            var handlerList =
                resultHandlers
                    .Where(handler =>
                        !string.IsNullOrWhiteSpace(
                            handler.EntityName))
                    .ToList();

            var duplicateEntityNames =
                handlerList
                    .GroupBy(
                        handler =>
                            handler.EntityName.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group =>
                        group.Count() > 1)
                    .Select(group =>
                        group.Key)
                    .ToArray();

            if (duplicateEntityNames.Length > 0)
            {
                throw new InvalidOperationException(
                    "Multiple local synchronization result " +
                    "handlers are registered for: " +
                    string.Join(
                        ", ",
                        duplicateEntityNames));
            }

            _resultHandlers =
                handlerList
                    .ToDictionary(
                        handler =>
                            handler.EntityName.Trim(),
                        handler =>
                            handler,
                        StringComparer.OrdinalIgnoreCase);
        }

        public async Task EnqueueAsync(
            string entityName,
            Guid localEntityId,
            string operation,
            string payloadJson,
            Guid? serverEntityId = null)
        {
            if (string.IsNullOrWhiteSpace(
                    entityName))
            {
                throw new ArgumentException(
                    "Entity name is required.",
                    nameof(entityName));
            }

            if (localEntityId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Local entity id is required.",
                    nameof(localEntityId));
            }

            if (string.IsNullOrWhiteSpace(
                    operation))
            {
                throw new ArgumentException(
                    "Operation is required.",
                    nameof(operation));
            }

            if (string.IsNullOrWhiteSpace(
                    payloadJson))
            {
                throw new ArgumentException(
                    "Payload JSON is required.",
                    nameof(payloadJson));
            }

            var item =
                new SyncQueueItem
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        _tenantContext
                            .GetRequiredTenantId(),

                    EntityName =
                        entityName.Trim(),

                    LocalEntityId =
                        localEntityId,

                    ServerEntityId =
                        serverEntityId,

                    Operation =
                        operation.Trim(),

                    PayloadJson =
                        payloadJson,

                    Status =
                        SyncQueueStatus.Pending,

                    Attempts =
                        0,

                    ErrorMessage =
                        null,

                    CreatedAtUtc =
                        DateTime.UtcNow,

                    LastAttemptAtUtc =
                        null,

                    ProcessedAtUtc =
                        null,

                    NextAttemptAtUtc =
                        null,

                    BatchId =
                        null,

                    LockedAtUtc =
                        null,

                    ClientOperationId =
                        Guid.NewGuid()
                };

            _db.SyncQueueItems.Add(
                item);

            await _db.SaveChangesAsync();
        }

        public async Task<List<SyncQueueItem>>
            GetPendingAsync(
                int take = 50)
        {
            take =
                Math.Clamp(
                    take,
                    1,
                    MaximumBatchSize);

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            var now =
                DateTime.UtcNow;

            return await _db.SyncQueueItems
                .AsNoTracking()
                .Where(item =>
                    item.TenantId == tenantId &&
                    (
                        item.Status ==
                            SyncQueueStatus.Pending ||
                        item.Status ==
                            SyncQueueStatus.Failed
                    ) &&
                    (
                        item.NextAttemptAtUtc == null ||
                        item.NextAttemptAtUtc <= now
                    ))
                .OrderBy(item =>
                    item.CreatedAtUtc)
                .ThenBy(item =>
                    item.Id)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<SyncQueueItem>>
            GetPendingBatchAsync(
                int batchSize = MaximumBatchSize,
                CancellationToken cancellationToken = default)
        {
            batchSize =
                Math.Clamp(
                    batchSize,
                    100,
                    MaximumBatchSize);

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            var now =
                DateTime.UtcNow;

            return await _db.SyncQueueItems
                .AsNoTracking()
                .Where(item =>
                    item.TenantId == tenantId &&
                    (
                        item.Status ==
                            SyncQueueStatus.Pending ||
                        item.Status ==
                            SyncQueueStatus.Failed
                    ) &&
                    (
                        item.NextAttemptAtUtc == null ||
                        item.NextAttemptAtUtc <= now
                    ))
                .OrderBy(item =>
                    item.CreatedAtUtc)
                .ThenBy(item =>
                    item.Id)
                .Take(batchSize)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<ClaimedSyncBatchResult>
            ClaimPendingBatchAsync(
                IReadOnlyCollection<string> entityNames,
                int batchSize = MaximumBatchSize,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                entityNames);

            var normalizedEntityNames =
                entityNames
                    .Where(name =>
                        !string.IsNullOrWhiteSpace(
                            name))
                    .Select(name =>
                        name.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            if (normalizedEntityNames.Length == 0)
            {
                throw new ArgumentException(
                    "At least one entity name is required.",
                    nameof(entityNames));
            }

            batchSize =
                Math.Clamp(
                    batchSize,
                    100,
                    MaximumBatchSize);

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            var now =
                DateTime.UtcNow;

            var batchId =
                Guid.NewGuid();

            await using var transaction =
                await _db.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                var items =
                    await _db.SyncQueueItems
                        .Where(item =>
                            item.TenantId == tenantId &&
                            normalizedEntityNames.Contains(
                                item.EntityName) &&
                            (
                                item.Status ==
                                    SyncQueueStatus.Pending ||
                                item.Status ==
                                    SyncQueueStatus.Failed
                            ) &&
                            (
                                item.NextAttemptAtUtc == null ||
                                item.NextAttemptAtUtc <= now
                            ))
                        .OrderBy(item =>
                            item.CreatedAtUtc)
                        .ThenBy(item =>
                            item.Id)
                        .Take(batchSize)
                        .ToListAsync(
                            cancellationToken);

                if (items.Count == 0)
                {
                    await transaction.CommitAsync(
                        cancellationToken);

                    return new ClaimedSyncBatchResult
                    {
                        BatchId =
                            Guid.Empty,

                        TenantId =
                            tenantId,

                        ClaimedAtUtc =
                            now,

                        Items =
                            new List<SyncQueueItem>()
                    };
                }

                foreach (var item in items)
                {
                    item.Status =
                        SyncQueueStatus.Processing;

                    item.BatchId =
                        batchId;

                    item.LockedAtUtc =
                        now;

                    item.LastAttemptAtUtc =
                        now;

                    item.NextAttemptAtUtc =
                        null;

                    item.Attempts++;

                    item.ErrorMessage =
                        null;
                }

                /*
                 * Tous les éléments passent en Processing
                 * dans une seule transaction SQLite.
                 */
                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                /*
                 * L'uploader ne doit pas conserver 5 000 entités
                 * suivies pendant l'appel réseau.
                 */
                foreach (var item in items)
                {
                    _db.Entry(item).State =
                        EntityState.Detached;
                }

                return new ClaimedSyncBatchResult
                {
                    BatchId =
                        batchId,

                    TenantId =
                        tenantId,

                    ClaimedAtUtc =
                        now,

                    Items =
                        items
                };
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                throw;
            }
        }

        public async Task ApplyBatchResultAsync(
            Guid batchId,
            SyncBatchResult result,
            CancellationToken cancellationToken = default)
        {
            if (batchId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Batch id is required.",
                    nameof(batchId));
            }

            ArgumentNullException.ThrowIfNull(
                result);

            if (result.BatchId != batchId)
            {
                throw new InvalidOperationException(
                    $"The server response batch '{result.BatchId}' " +
                    $"does not match the local batch '{batchId}'.");
            }

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            var resultItems =
                result.Items ??
                new List<SyncBatchItemResult>();

            var duplicateQueueItemIds =
                resultItems
                    .GroupBy(item =>
                        item.QueueItemId)
                    .Where(group =>
                        group.Count() > 1)
                    .Select(group =>
                        group.Key)
                    .ToArray();

            if (duplicateQueueItemIds.Length > 0)
            {
                throw new InvalidOperationException(
                    "The server returned duplicate queue item ids: " +
                    string.Join(
                        ", ",
                        duplicateQueueItemIds));
            }

            var resultsByQueueItemId =
                resultItems
                    .ToDictionary(item =>
                        item.QueueItemId);

            var now =
                DateTime.UtcNow;

            await using var transaction =
                await _db.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                var queueItems =
                    await _db.SyncQueueItems
                        .Where(item =>
                            item.TenantId == tenantId &&
                            item.BatchId == batchId &&
                            item.Status ==
                                SyncQueueStatus.Processing)
                        .OrderBy(item =>
                            item.CreatedAtUtc)
                        .ThenBy(item =>
                            item.Id)
                        .ToListAsync(
                            cancellationToken);

                if (queueItems.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No processing queue items were found for " +
                        $"batch '{batchId}'.");
                }

                var claimedQueueItemIds =
                    queueItems
                        .Select(item =>
                            item.Id)
                        .ToHashSet();

                var unknownQueueItemIds =
                    resultItems
                        .Where(item =>
                            !claimedQueueItemIds.Contains(
                                item.QueueItemId))
                        .Select(item =>
                            item.QueueItemId)
                        .Distinct()
                        .ToArray();

                if (unknownQueueItemIds.Length > 0)
                {
                    throw new InvalidOperationException(
                        "The server returned queue items that do not " +
                        "belong to the claimed batch: " +
                        string.Join(
                            ", ",
                            unknownQueueItemIds));
                }

                foreach (var queueItem in queueItems)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    if (!resultsByQueueItemId.TryGetValue(
                            queueItem.Id,
                            out var itemResult))
                    {
                        MarkBatchItemFailed(
                            queueItem,
                            "The server did not return a result for " +
                            $"queue item '{queueItem.Id}'.",
                            now);
                    }
                    else if (itemResult.ClientOperationId !=
                             queueItem.ClientOperationId)
                    {
                        MarkBatchItemConflict(
                            queueItem,
                            "The returned ClientOperationId does not " +
                            "match the local queue item.",
                            now);
                    }
                    else if (itemResult.IsSuccessful)
                    {
                        if (!_resultHandlers.TryGetValue(
                                queueItem.EntityName,
                                out var resultHandler))
                        {
                            MarkBatchItemConflict(
                                queueItem,
                                "No local synchronization result handler " +
                                $"is registered for entity " +
                                $"'{queueItem.EntityName}'.",
                                now);
                        }
                        else
                        {
                            await resultHandler.ApplyAsync(
                                queueItem,
                                itemResult,
                                cancellationToken);

                            /*
                             * The local result handler can detect a conflict while
                             * reconciling the server identifier.
                             */
                            if (itemResult.IsConflict)
                            {
                                MarkBatchItemConflict(
                                    queueItem,
                                    itemResult.ErrorMessage ??
                                    "The server identifier is already linked " +
                                    "to another local entity.",
                                    now);
                            }
                            else if (!itemResult.IsSuccessful)
                            {
                                MarkBatchItemFailed(
                                    queueItem,
                                    itemResult.ErrorMessage ??
                                    "The local synchronization result could " +
                                    "not be applied.",
                                    now);
                            }
                            else
                            {
                                queueItem.Status =
                                    SyncQueueStatus.Done;

                                queueItem.ServerEntityId =
                                    itemResult.ServerEntityId ??
                                    queueItem.ServerEntityId;

                                queueItem.ErrorMessage =
                                    null;

                                queueItem.ProcessedAtUtc =
                                    now;

                                queueItem.NextAttemptAtUtc =
                                    null;
                            }
                        }
                    }
                    else if (itemResult.IsConflict)
                    {
                        MarkBatchItemConflict(
                            queueItem,
                            itemResult.ErrorMessage ??
                            "The server reported a synchronization " +
                            "conflict.",
                            now);
                    }
                    else
                    {
                        MarkBatchItemFailed(
                            queueItem,
                            itemResult.ErrorMessage ??
                            "The server could not process the " +
                            "synchronization operation.",
                            now);
                    }

                    /*
                     * Every terminal branch releases the local claim.
                     * The item can be retried only if its status is Failed.
                     */
                    queueItem.BatchId =
                        null;

                    queueItem.LockedAtUtc =
                        null;
                }

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                throw;
            }
        }

        public async Task ReleaseBatchAsync(
            Guid batchId,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            if (batchId == Guid.Empty)
            {
                return;
            }

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            var now =
                DateTime.UtcNow;

            var queueItems =
                await _db.SyncQueueItems
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.BatchId == batchId &&
                        item.Status ==
                            SyncQueueStatus.Processing)
                    .ToListAsync(
                        cancellationToken);

            if (queueItems.Count == 0)
            {
                return;
            }

            foreach (var queueItem in queueItems)
            {
                MarkBatchItemFailed(
                    queueItem,
                    errorMessage,
                    now);

                queueItem.BatchId =
                    null;

                queueItem.LockedAtUtc =
                    null;
            }

            await _db.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<List<SyncQueueItem>>
            GetConflictsAsync()
        {
            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            return await _db.SyncQueueItems
                .AsNoTracking()
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.Status ==
                        SyncQueueStatus.Conflict)
                .OrderByDescending(item =>
                    item.CreatedAtUtc)
                .ThenByDescending(item =>
                    item.Id)
                .ToListAsync();
        }

        public async Task MarkProcessingAsync(
            Guid id)
        {
            var item =
                await GetRequiredAsync(
                    id);

            var now =
                DateTime.UtcNow;

            item.Status =
                SyncQueueStatus.Processing;

            item.Attempts++;

            item.LastAttemptAtUtc =
                now;

            item.LockedAtUtc =
                now;

            item.NextAttemptAtUtc =
                null;

            item.ErrorMessage =
                null;

            await _db.SaveChangesAsync();
        }

        public async Task MarkDoneAsync(
            Guid id,
            Guid? serverEntityId = null)
        {
            var item =
                await GetRequiredAsync(
                    id);

            item.Status =
                SyncQueueStatus.Done;

            item.ServerEntityId =
                serverEntityId ??
                item.ServerEntityId;

            item.ProcessedAtUtc =
                DateTime.UtcNow;

            item.ErrorMessage =
                null;

            item.NextAttemptAtUtc =
                null;

            item.BatchId =
                null;

            item.LockedAtUtc =
                null;

            await _db.SaveChangesAsync();
        }

        public async Task MarkFailedAsync(
            Guid id,
            string errorMessage)
        {
            var item =
                await GetRequiredAsync(
                    id);

            var now =
                DateTime.UtcNow;

            item.Status =
                SyncQueueStatus.Failed;

            item.ErrorMessage =
                TruncateError(
                    errorMessage);

            item.LastAttemptAtUtc =
                now;

            item.ProcessedAtUtc =
                null;

            item.NextAttemptAtUtc =
                CalculateNextAttempt(
                    now,
                    item.Attempts);

            item.BatchId =
                null;

            item.LockedAtUtc =
                null;

            await _db.SaveChangesAsync();
        }

        public async Task MarkConflictAsync(
            Guid id,
            string errorMessage)
        {
            var item =
                await GetRequiredAsync(
                    id);

            var now =
                DateTime.UtcNow;

            item.Status =
                SyncQueueStatus.Conflict;

            item.ErrorMessage =
                TruncateError(
                    errorMessage);

            item.LastAttemptAtUtc =
                now;

            item.ProcessedAtUtc =
                now;

            item.NextAttemptAtUtc =
                null;

            item.BatchId =
                null;

            item.LockedAtUtc =
                null;

            await _db.SaveChangesAsync();
        }

        private async Task<SyncQueueItem>
            GetRequiredAsync(
                Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "Sync queue item id is required.",
                    nameof(id));
            }

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            var item =
                await _db.SyncQueueItems
                    .FirstOrDefaultAsync(queueItem =>
                        queueItem.TenantId ==
                            tenantId &&
                        queueItem.Id == id);

            if (item == null)
            {
                throw new InvalidOperationException(
                    $"Sync queue item '{id}' was not found.");
            }

            return item;
        }

        private static DateTime CalculateNextAttempt(
            DateTime now,
            int attempts)
        {
            var delay =
                attempts switch
                {
                    <= 1 =>
                        TimeSpan.FromSeconds(2),

                    2 =>
                        TimeSpan.FromSeconds(5),

                    3 =>
                        TimeSpan.FromSeconds(15),

                    4 =>
                        TimeSpan.FromSeconds(30),

                    5 =>
                        TimeSpan.FromMinutes(1),

                    _ =>
                        TimeSpan.FromMinutes(5)
                };

            return now.Add(
                delay);
        }

        private static void MarkBatchItemFailed(
            SyncQueueItem queueItem,
            string? errorMessage,
            DateTime now)
        {
            queueItem.Status =
                SyncQueueStatus.Failed;

            queueItem.ErrorMessage =
                TruncateError(
                    errorMessage);

            queueItem.ProcessedAtUtc =
                null;

            queueItem.NextAttemptAtUtc =
                CalculateNextAttempt(
                    now,
                    queueItem.Attempts);
        }

        private static void MarkBatchItemConflict(
            SyncQueueItem queueItem,
            string? errorMessage,
            DateTime now)
        {
            queueItem.Status =
                SyncQueueStatus.Conflict;

            queueItem.ErrorMessage =
                TruncateError(
                    errorMessage);

            queueItem.ProcessedAtUtc =
                now;

            queueItem.NextAttemptAtUtc =
                null;
        }

        private static string TruncateError(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return
                    "Synchronization failed.";
            }

            value =
                value.Trim();

            return value.Length <=
                   MaximumErrorLength
                ? value
                : value[..MaximumErrorLength];
        }
    }
}