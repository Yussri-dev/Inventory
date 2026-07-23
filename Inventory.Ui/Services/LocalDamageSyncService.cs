using Inventory.Dto.Damages.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services;

public sealed class LocalDamageSyncService
    : ILocalDamageSyncService
{
    private const string DamageEntityName = "Damage";
    private const string FullSyncMode = "Full";

    private const int PageSize = 100;
    private const int MaximumPages = 10_000;

    private readonly PosLocalDbContext _db;
    private readonly IDamageApi _damageApi;
    private readonly ILocalTenantContext _tenantContext;
    private readonly ILogger<LocalDamageSyncService> _logger;

    public LocalDamageSyncService(
        PosLocalDbContext db,
        IDamageApi damageApi,
        ILocalTenantContext tenantContext,
        ILogger<LocalDamageSyncService> logger)
    {
        _db = db;
        _damageApi = damageApi;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task FullSyncAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        try
        {
            /*
             * Always download before starting the SQLite transaction.
             * We must not keep a local transaction open while waiting
             * for an HTTP request.
             */
            var serverDamages =
                await DownloadAllDamagesAsync(
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var synchronizedAtUtc =
                    DateTime.UtcNow;

                /*
                 * Local damages with unfinished queue operations must
                 * not be overwritten by the server pull.
                 */
                var pendingLocalDamageIds =
                    await _db.SyncQueueItems
                        .AsNoTracking()
                        .Where(queueItem =>
                            queueItem.TenantId == tenantId &&
                            queueItem.EntityName ==
                                DamageEntityName &&
                            queueItem.Status !=
                                SyncQueueStatus.Done)
                        .Select(queueItem =>
                            queueItem.LocalEntityId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                var pendingLocalDamageSet =
                    pendingLocalDamageIds.ToHashSet();

                /*
                 * DamageResult.ProductId contains the server Product ID.
                 *
                 * Historical damages may still reference a locally
                 * deleted product, so IsDeletedLocally is deliberately
                 * not used here.
                 */
                var localProducts =
                    await _db.Products
                        .AsNoTracking()
                        .Where(product =>
                            product.TenantId == tenantId &&
                            product.ServerId.HasValue &&
                            product.ServerId.Value != Guid.Empty)
                        .ToListAsync(cancellationToken);

                var productsByServerId =
                    localProducts
                        .GroupBy(product =>
                            product.ServerId!.Value)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());

                var localDamages =
                    await _db.Damages
                        .Where(damage =>
                            damage.TenantId == tenantId)
                        .ToListAsync(cancellationToken);

                var damagesByServerId =
                    localDamages
                        .Where(damage =>
                            damage.ServerId.HasValue &&
                            damage.ServerId.Value != Guid.Empty)
                        .GroupBy(damage =>
                            damage.ServerId!.Value)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());

                var processedCount = 0;
                var skippedCount = 0;

                foreach (var serverDamage in serverDamages)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ValidateServerDamage(serverDamage);

                    if (!productsByServerId.TryGetValue(
                            serverDamage.ProductId,
                            out var localProduct))
                    {
                        /*
                         * Do not fail the complete synchronization because
                         * one historical product is missing locally.
                         */
                        skippedCount++;

                        _logger.LogWarning(
                            "Damage {DamageId} was skipped because server " +
                            "Product {ProductId} does not exist locally.",
                            serverDamage.Id,
                            serverDamage.ProductId);

                        continue;
                    }

                    damagesByServerId.TryGetValue(
                        serverDamage.Id,
                        out var localDamage);

                    if (localDamage != null &&
                        pendingLocalDamageSet.Contains(
                            localDamage.Id))
                    {
                        skippedCount++;

                        _logger.LogDebug(
                            "Server Damage {ServerDamageId} was skipped " +
                            "because local Damage {LocalDamageId} has " +
                            "pending changes.",
                            serverDamage.Id,
                            localDamage.Id);

                        continue;
                    }

                    if (localDamage == null)
                    {
                        localDamage =
                            new LocalDamage
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                CreatedAtUtc =
                                    synchronizedAtUtc
                            };

                        _db.Damages.Add(localDamage);

                        localDamages.Add(localDamage);
                    }

                    ApplyServerDamage(
                        localDamage,
                        serverDamage,
                        localProduct,
                        tenantId,
                        synchronizedAtUtc);

                    damagesByServerId[serverDamage.Id] =
                        localDamage;

                    processedCount++;
                }

                await MarkSyncSucceededAsync(
                    tenantId,
                    synchronizedAtUtc,
                    cancellationToken);

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Damage synchronization completed for tenant " +
                    "{TenantId}. Processed: {ProcessedCount}; " +
                    "skipped: {SkippedCount}; received: {ReceivedCount}.",
                    tenantId,
                    processedCount,
                    skippedCount,
                    serverDamages.Count);
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await RecordSyncFailureAsync(
                tenantId,
                exception);

            _logger.LogError(
                exception,
                "Damage synchronization failed for tenant {TenantId}.",
                tenantId);

            throw;
        }
    }

    public async Task UpsertFromServerAsync(
        DamageResult serverDamage,
        Guid? originatingLocalId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serverDamage);

        ValidateServerDamage(serverDamage);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var synchronizedAtUtc =
                DateTime.UtcNow;

            var localProduct =
                await _db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        product =>
                            product.TenantId == tenantId &&
                            product.ServerId ==
                                serverDamage.ProductId,
                        cancellationToken);

            if (localProduct == null)
            {
                throw new InvalidOperationException(
                    $"The local Product corresponding to server " +
                    $"Product '{serverDamage.ProductId}' was not found.");
            }

            LocalDamage? localDamage = null;

            /*
             * The originating local ID is used after uploading an
             * offline damage. This prevents the creation of a second
             * SQLite row for the server response.
             */
            if (originatingLocalId.HasValue &&
                originatingLocalId.Value != Guid.Empty)
            {
                localDamage =
                    await _db.Damages
                        .FirstOrDefaultAsync(
                            damage =>
                                damage.TenantId == tenantId &&
                                damage.Id ==
                                    originatingLocalId.Value,
                            cancellationToken);
            }

            /*
             * Fallback for normal server pull or an already reconciled
             * local record.
             */
            localDamage ??=
                await _db.Damages
                    .FirstOrDefaultAsync(
                        damage =>
                            damage.TenantId == tenantId &&
                            damage.ServerId ==
                                serverDamage.Id,
                        cancellationToken);

            if (localDamage == null)
            {
                localDamage =
                    new LocalDamage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        CreatedAtUtc =
                            synchronizedAtUtc
                    };

                _db.Damages.Add(localDamage);
            }

            ApplyServerDamage(
                localDamage,
                serverDamage,
                localProduct,
                tenantId,
                synchronizedAtUtc);

            if (originatingLocalId.HasValue &&
                originatingLocalId.Value != Guid.Empty)
            {
                await CompleteQueueItemsAsync(
                    tenantId,
                    localDamage.Id,
                    synchronizedAtUtc,
                    cancellationToken);
            }

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    public async Task<bool> HasInitialSyncCompletedAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        return await _db.SyncTableStates
            .AsNoTracking()
            .AnyAsync(
                state =>
                    state.TenantId == tenantId &&
                    state.EntityName ==
                        DamageEntityName &&
                    state.InitialSyncCompleted,
                cancellationToken);
    }

    private async Task<List<DamageResult>>
        DownloadAllDamagesAsync(
            CancellationToken cancellationToken)
    {
        var damages =
            new List<DamageResult>();

        for (var page = 1;
             page <= MaximumPages;
             page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response =
                await _damageApi.Search(
                    new DamageQuery
                    {
                        Page = page,
                        PageSize = PageSize
                    },
                    cancellationToken);

            var pageItems =
                response.Items?.ToList()
                ?? new List<DamageResult>();

            if (pageItems.Count == 0)
            {
                break;
            }

            damages.AddRange(pageItems);

            if (response.TotalCount > 0 &&
                damages.Count >= response.TotalCount)
            {
                break;
            }

            if (pageItems.Count < PageSize)
            {
                break;
            }

            if (page == MaximumPages)
            {
                throw new InvalidOperationException(
                    "Damage synchronization exceeded the maximum " +
                    $"number of pages ({MaximumPages}).");
            }
        }

        /*
         * Protect against duplicate rows returned by a badly paginated
         * endpoint.
         */
        return damages
            .Where(damage =>
                damage.Id != Guid.Empty)
            .GroupBy(damage =>
                damage.Id)
            .Select(group =>
                group.Last())
            .ToList();
    }

    private static void ApplyServerDamage(
        LocalDamage localDamage,
        DamageResult serverDamage,
        LocalProduct localProduct,
        Guid tenantId,
        DateTime synchronizedAtUtc)
    {
        localDamage.TenantId =
            tenantId;

        localDamage.ServerId =
            serverDamage.Id;

        localDamage.DamageNumber =
            string.IsNullOrWhiteSpace(
                serverDamage.DamageNumber)
                ? $"DMG-{serverDamage.Id:N}"
                : serverDamage.DamageNumber.Trim();

        localDamage.ProductLocalId =
            localProduct.Id;

        localDamage.ProductServerId =
            serverDamage.ProductId;

        localDamage.ProductName =
            string.IsNullOrWhiteSpace(
                serverDamage.ProductName)
                ? localProduct.Name
                : serverDamage.ProductName.Trim();

        localDamage.Quantity =
            serverDamage.Quantity;

        localDamage.EstimatedValue =
            serverDamage.EstimatedValue;

        localDamage.Reason =
            NormalizeNullable(
                serverDamage.Reason);

        localDamage.DamageDateUtc =
            NormalizeUtc(
                serverDamage.DamageDate);

        /*
         * A server record must not appear in the local draft list.
         *
         * This method deliberately does not update LocalStock:
         * stock is reconciled separately by LocalStockSyncService.
         */
        localDamage.LocalStatus =
            LocalDamageStatus.Synced;

        localDamage.ServerStatus =
            serverDamage.Status.ToString();

        localDamage.IsDeleted =
            false;

        localDamage.SyncStatus =
            SyncQueueStatus.Done;

        localDamage.DeletedAtUtc =
            synchronizedAtUtc;

        localDamage.ModifiedAtUtc =
            synchronizedAtUtc;

        localDamage.LastSyncedAtUtc =
            synchronizedAtUtc;

        if (localDamage.CreatedAtUtc == default)
        {
            localDamage.CreatedAtUtc =
                synchronizedAtUtc;
        }
    }

    private async Task CompleteQueueItemsAsync(
        Guid tenantId,
        Guid localDamageId,
        DateTime processedAtUtc,
        CancellationToken cancellationToken)
    {
        var queueItems =
            await _db.SyncQueueItems
                .Where(queueItem =>
                    queueItem.TenantId == tenantId &&
                    queueItem.EntityName ==
                        DamageEntityName &&
                    queueItem.LocalEntityId ==
                        localDamageId &&
                    queueItem.Status !=
                        SyncQueueStatus.Done)
                .ToListAsync(cancellationToken);

        foreach (var queueItem in queueItems)
        {
            queueItem.Status =
                SyncQueueStatus.Done;

            queueItem.ProcessedAtUtc =
                processedAtUtc;

            queueItem.ErrorMessage =
                null;
        }
    }

    private async Task MarkSyncSucceededAsync(
        Guid tenantId,
        DateTime synchronizedAtUtc,
        CancellationToken cancellationToken)
    {
        var state =
            await _db.SyncTableStates
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.EntityName ==
                            DamageEntityName,
                    cancellationToken);

        if (state == null)
        {
            state =
                new SyncTableStateLocal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    EntityName =
                        DamageEntityName
                };

            _db.SyncTableStates.Add(state);
        }

        state.Syncmode =
            FullSyncMode;

        state.InitialSyncCompleted =
            true;

        state.LastSuccessfulSyncAtUtc =
            synchronizedAtUtc;

        state.LastError =
            null;

        state.ContinuationToken =
            null;
    }

    private async Task RecordSyncFailureAsync(
        Guid tenantId,
        Exception exception)
    {
        try
        {
            /*
             * Remove failed tracked changes before saving only the
             * synchronization error state.
             */
            _db.ChangeTracker.Clear();

            var state =
                await _db.SyncTableStates
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.EntityName ==
                                DamageEntityName,
                        CancellationToken.None);

            if (state == null)
            {
                state =
                    new SyncTableStateLocal
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        EntityName =
                            DamageEntityName,
                        Syncmode =
                            FullSyncMode,
                        InitialSyncCompleted =
                            false
                    };

                _db.SyncTableStates.Add(state);
            }

            state.Syncmode =
                FullSyncMode;

            state.LastError =
                Truncate(
                    exception
                        .GetBaseException()
                        .Message,
                    2000);

            await _db.SaveChangesAsync(
                CancellationToken.None);
        }
        catch (Exception stateException)
        {
            _logger.LogWarning(
                stateException,
                "Could not persist Damage synchronization failure " +
                "state for tenant {TenantId}.",
                tenantId);
        }
    }

    private static void ValidateServerDamage(
        DamageResult damage)
    {
        ArgumentNullException.ThrowIfNull(damage);

        if (damage.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The server returned a Damage without an Id.");
        }

        if (damage.ProductId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Damage '{damage.Id}' does not contain a ProductId.");
        }

        if (damage.Quantity <= 0)
        {
            throw new InvalidOperationException(
                $"Damage '{damage.Id}' contains an invalid quantity.");
        }

        if (damage.EstimatedValue < 0)
        {
            throw new InvalidOperationException(
                $"Damage '{damage.Id}' contains a negative " +
                "estimated value.");
        }

        if (damage.DamageDate == default)
        {
            throw new InvalidOperationException(
                $"Damage '{damage.Id}' does not contain a valid date.");
        }
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc)
        };
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string Truncate(
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}