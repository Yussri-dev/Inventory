using Inventory.Dto.Queries;
using Inventory.Dto.Suppliers.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services;

public sealed class LocalSupplierSyncService
    : ILocalSupplierSyncService
{
    private const string SupplierEntityName =
        "Supplier";

    private const string FullSyncMode =
        "Full";

    private const int PageSize = 100;
    private const int MaximumPages = 10_000;

    private readonly PosLocalDbContext _db;
    private readonly ISupplierApi _supplierApi;
    private readonly ILocalTenantContext _tenantContext;
    private readonly ILogger<LocalSupplierSyncService> _logger;

    public LocalSupplierSyncService(
        PosLocalDbContext db,
        ISupplierApi supplierApi,
        ILocalTenantContext tenantContext,
        ILogger<LocalSupplierSyncService> logger)
    {
        _db = db;
        _supplierApi = supplierApi;
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
            var serverSuppliers =
                await DownloadAllSuppliersAsync(
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var now =
                    DateTime.UtcNow;

                var pendingIds =
                    await _db.SyncQueueItems
                        .AsNoTracking()
                        .Where(item =>
                            item.TenantId == tenantId &&
                            item.EntityName ==
                                SupplierEntityName &&
                            item.Status !=
                                SyncQueueStatus.Done)
                        .Select(item =>
                            item.LocalEntityId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                var pendingSet =
                    pendingIds.ToHashSet();

                var localSuppliers =
                    await _db.Suppliers
                        .Where(supplier =>
                            supplier.TenantId == tenantId)
                        .ToListAsync(cancellationToken);

                var byServerId =
                    localSuppliers
                        .Where(supplier =>
                            supplier.ServerId.HasValue &&
                            supplier.ServerId.Value !=
                            Guid.Empty)
                        .GroupBy(supplier =>
                            supplier.ServerId!.Value)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());

                foreach (var serverSupplier in serverSuppliers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ValidateServerSupplier(
                        serverSupplier);

                    byServerId.TryGetValue(
                        serverSupplier.Id,
                        out var localSupplier);

                    if (localSupplier != null &&
                        pendingSet.Contains(localSupplier.Id))
                    {
                        _logger.LogDebug(
                            "Supplier {ServerId} was skipped because " +
                            "local Supplier {LocalId} has pending changes.",
                            serverSupplier.Id,
                            localSupplier.Id);

                        continue;
                    }

                    if (localSupplier == null)
                    {
                        localSupplier =
                            new LocalSupplier
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                CreatedAtUtc = now
                            };

                        _db.Suppliers.Add(
                            localSupplier);

                        localSuppliers.Add(
                            localSupplier);
                    }

                    ApplyServerSupplier(
                        localSupplier,
                        serverSupplier,
                        tenantId,
                        now);

                    byServerId[serverSupplier.Id] =
                        localSupplier;
                }

                await MarkSyncSucceededAsync(
                    tenantId,
                    now,
                    cancellationToken);

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Supplier synchronization completed for tenant " +
                    "{TenantId}. {Count} suppliers were processed.",
                    tenantId,
                    serverSuppliers.Count);
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
                "Supplier synchronization failed for tenant {TenantId}.",
                tenantId);

            throw;
        }
    }

    public async Task UpsertFromServerAsync(
        SupplierResult serverSupplier,
        Guid? originatingLocalId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateServerSupplier(
            serverSupplier);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var now =
            DateTime.UtcNow;

        LocalSupplier? localSupplier = null;

        if (originatingLocalId.HasValue &&
            originatingLocalId.Value != Guid.Empty)
        {
            localSupplier =
                await _db.Suppliers
                    .FirstOrDefaultAsync(
                        supplier =>
                            supplier.TenantId == tenantId &&
                            supplier.Id ==
                                originatingLocalId.Value,
                        cancellationToken);
        }

        localSupplier ??=
            await _db.Suppliers
                .FirstOrDefaultAsync(
                    supplier =>
                        supplier.TenantId == tenantId &&
                        supplier.ServerId ==
                            serverSupplier.Id,
                    cancellationToken);

        if (localSupplier == null)
        {
            localSupplier =
                new LocalSupplier
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CreatedAtUtc = now
                };

            _db.Suppliers.Add(
                localSupplier);
        }

        ApplyServerSupplier(
            localSupplier,
            serverSupplier,
            tenantId,
            now);

        if (originatingLocalId.HasValue)
        {
            await CompleteQueueItemsAsync(
                tenantId,
                localSupplier.Id,
                now,
                cancellationToken);
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task MarkDeletedFromServerAsync(
        Guid serverSupplierId,
        CancellationToken cancellationToken = default)
    {
        if (serverSupplierId == Guid.Empty)
        {
            throw new ArgumentException(
                "Server supplier id is required.",
                nameof(serverSupplierId));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var localSupplier =
            await _db.Suppliers
                .FirstOrDefaultAsync(
                    supplier =>
                        supplier.TenantId == tenantId &&
                        supplier.ServerId ==
                            serverSupplierId,
                    cancellationToken);

        if (localSupplier == null)
            return;

        var now =
            DateTime.UtcNow;

        localSupplier.IsDeleted = true;
        localSupplier.IsActive = false;
        localSupplier.DeletedAtUtc = now;
        localSupplier.ModifiedAtUtc = now;
        localSupplier.LastSyncedAtUtc = now;
        localSupplier.SyncStatus =
            SyncQueueStatus.Done;

        await CompleteQueueItemsAsync(
            tenantId,
            localSupplier.Id,
            now,
            cancellationToken);

        await _db.SaveChangesAsync(
            cancellationToken);
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
                        SupplierEntityName &&
                    state.InitialSyncCompleted,
                cancellationToken);
    }

    private async Task<List<SupplierResult>>
        DownloadAllSuppliersAsync(
            CancellationToken cancellationToken)
    {
        var suppliers =
            new List<SupplierResult>();

        for (var page = 1;
             page <= MaximumPages;
             page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response =
                await _supplierApi.Search(
                    new SupplierQuery
                    {
                        Page = page,
                        PageSize = PageSize,
                        SortBy = "name",
                        Desc = false
                    },
                    cancellationToken);

            var pageItems =
                response.Items?.ToList()
                ?? new List<SupplierResult>();

            if (pageItems.Count == 0)
                break;

            suppliers.AddRange(pageItems);

            if (response.TotalCount > 0 &&
                suppliers.Count >= response.TotalCount)
            {
                break;
            }

            if (pageItems.Count < PageSize)
                break;

            if (page == MaximumPages)
            {
                throw new InvalidOperationException(
                    "Supplier synchronization exceeded " +
                    "the maximum number of pages.");
            }
        }

        return suppliers
            .Where(supplier =>
                supplier.Id != Guid.Empty)
            .GroupBy(supplier =>
                supplier.Id)
            .Select(group =>
                group.Last())
            .ToList();
    }

    private static void ApplyServerSupplier(
        LocalSupplier localSupplier,
        SupplierResult serverSupplier,
        Guid tenantId,
        DateTime now)
    {
        localSupplier.TenantId =
            tenantId;

        localSupplier.ServerId =
            serverSupplier.Id;

        localSupplier.Name =
            serverSupplier.Name.Trim();

        localSupplier.ContactPerson =
            NormalizeNullable(
                serverSupplier.ContactPerson);

        localSupplier.Email =
            NormalizeNullable(
                serverSupplier.Email);

        localSupplier.Phone =
            NormalizeNullable(
                serverSupplier.Phone);

        localSupplier.Address =
            NormalizeNullable(
                serverSupplier.Address);

        localSupplier.City =
            NormalizeNullable(
                serverSupplier.City);

        localSupplier.PostalCode =
            NormalizeNullable(
                serverSupplier.PostalCode);

        localSupplier.Country =
            NormalizeNullable(
                serverSupplier.Country);

        localSupplier.TaxNumber =
            NormalizeNullable(
                serverSupplier.TaxNumber);

        localSupplier.PaymentTermsDays =
            serverSupplier.PaymentTermsDays;

        localSupplier.BankAccount =
            NormalizeNullable(
                serverSupplier.BankAccount);

        localSupplier.IsActive =
            serverSupplier.IsActive;

        localSupplier.Notes =
            NormalizeNullable(
                serverSupplier.Notes);

        /*
         * Ajoute ce mapping lorsque SupplierResult expose
         * CurrentBalance :
         *
         * localSupplier.CurrentBalance =
         *     serverSupplier.CurrentBalance;
         */

        localSupplier.IsDeleted = false;
        localSupplier.DeletedAtUtc = null;

        localSupplier.SyncStatus =
            SyncQueueStatus.Done;

        localSupplier.LastSyncedAtUtc =
            now;

        localSupplier.ModifiedAtUtc =
            now;

        if (localSupplier.CreatedAtUtc == default)
        {
            localSupplier.CreatedAtUtc =
                now;
        }
    }

    private async Task CompleteQueueItemsAsync(
        Guid tenantId,
        Guid localSupplierId,
        DateTime processedAtUtc,
        CancellationToken cancellationToken)
    {
        var queueItems =
            await _db.SyncQueueItems
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.EntityName ==
                        SupplierEntityName &&
                    item.LocalEntityId ==
                        localSupplierId &&
                    item.Status !=
                        SyncQueueStatus.Done)
                .ToListAsync(cancellationToken);

        foreach (var item in queueItems)
        {
            item.Status =
                SyncQueueStatus.Done;

            item.ProcessedAtUtc =
                processedAtUtc;

            item.ErrorMessage =
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
                            SupplierEntityName,
                    cancellationToken);

        if (state == null)
        {
            state =
                new SyncTableStateLocal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    EntityName = SupplierEntityName,
                    Syncmode = FullSyncMode
                };

            _db.SyncTableStates.Add(
                state);
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
            _db.ChangeTracker.Clear();

            var state =
                await _db.SyncTableStates
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.EntityName ==
                                SupplierEntityName,
                        CancellationToken.None);

            if (state == null)
            {
                state =
                    new SyncTableStateLocal
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        EntityName = SupplierEntityName,
                        Syncmode = FullSyncMode,
                        InitialSyncCompleted = false
                    };

                _db.SyncTableStates.Add(
                    state);
            }

            state.LastError =
                Truncate(
                    exception.GetBaseException().Message,
                    2000);

            await _db.SaveChangesAsync(
                CancellationToken.None);
        }
        catch (Exception stateException)
        {
            _logger.LogWarning(
                stateException,
                "Could not persist Supplier sync failure state " +
                "for tenant {TenantId}.",
                tenantId);
        }
    }

    private static void ValidateServerSupplier(
        SupplierResult supplier)
    {
        ArgumentNullException.ThrowIfNull(
            supplier);

        if (supplier.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The server returned a Supplier without an Id.");
        }

        if (string.IsNullOrWhiteSpace(supplier.Name))
        {
            throw new InvalidOperationException(
                $"Server Supplier '{supplier.Id}' has no name.");
        }

        if (supplier.PaymentTermsDays < 0)
        {
            throw new InvalidOperationException(
                $"Server Supplier '{supplier.Id}' has invalid " +
                "payment terms.");
        }
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}