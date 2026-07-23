using Inventory.Dto.Customers.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services;

public sealed class LocalCustomerSyncService
    : ILocalCustomerSyncService
{
    private const string CustomerEntityName =
        "Customer";

    private const string CustomerTransactionEntityName =
        "CustomerTransaction";

    private const string FullSyncMode =
        "Full";

    private const int PageSize =
        100;

    private const int MaximumPages =
        10_000;

    private readonly PosLocalDbContext _db;
    private readonly ICustomerApi _customerApi;
    private readonly ILocalTenantContext _tenantContext;
    private readonly ILogger<LocalCustomerSyncService> _logger;

    public LocalCustomerSyncService(
        PosLocalDbContext db,
        ICustomerApi customerApi,
        ILocalTenantContext tenantContext,
        ILogger<LocalCustomerSyncService> logger)
    {
        _db = db;
        _customerApi = customerApi;
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
            var serverCustomers =
                await DownloadAllCustomersAsync(
                    cancellationToken);

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var now =
                    DateTime.UtcNow;

                /*
                 * Profile mutations protect the complete LocalCustomer row.
                 */
                var pendingCustomerProfileIds =
                    await _db.SyncQueueItems
                        .AsNoTracking()
                        .Where(item =>
                            item.TenantId == tenantId &&
                            item.EntityName == CustomerEntityName &&
                            item.Status != SyncQueueStatus.Done)
                        .Select(item =>
                            item.LocalEntityId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                var pendingProfileSet =
                    pendingCustomerProfileIds.ToHashSet();

                /*
                 * Financial mutations protect only CurrentBalance.
                 * Profile fields may still be refreshed from the server.
                 */
                var pendingBalanceCustomerIds =
                    await _db.CustomerTransactions
                        .AsNoTracking()
                        .Where(transaction =>
                            transaction.TenantId == tenantId &&
                            transaction.UploadRequired &&
                            transaction.SyncStatus !=
                                SyncQueueStatus.Done)
                        .Select(transaction =>
                            transaction.CustomerLocalId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                var pendingBalanceSet =
                    pendingBalanceCustomerIds.ToHashSet();

                var localCustomers =
                    await _db.Customers
                        .Where(customer =>
                            customer.TenantId == tenantId)
                        .ToListAsync(cancellationToken);

                var byServerId =
                    localCustomers
                        .Where(customer =>
                            customer.ServerId.HasValue &&
                            customer.ServerId.Value != Guid.Empty)
                        .GroupBy(customer =>
                            customer.ServerId!.Value)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());

                foreach (var serverCustomer in serverCustomers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ValidateServerCustomer(serverCustomer);

                    byServerId.TryGetValue(
                        serverCustomer.Id,
                        out var localCustomer);

                    if (localCustomer != null &&
                        pendingProfileSet.Contains(localCustomer.Id))
                    {
                        _logger.LogDebug(
                            "Customer {ServerId} was skipped because " +
                            "local Customer {LocalId} has pending changes.",
                            serverCustomer.Id,
                            localCustomer.Id);

                        continue;
                    }

                    if (localCustomer == null)
                    {
                        localCustomer =
                            new LocalCustomer
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                CreatedAtUtc = now
                            };

                        _db.Customers.Add(localCustomer);
                        localCustomers.Add(localCustomer);
                    }

                    ApplyServerCustomer(
                        localCustomer,
                        serverCustomer,
                        tenantId,
                        now,
                        preserveCurrentBalance:
                            pendingBalanceSet.Contains(
                                localCustomer.Id));

                    byServerId[serverCustomer.Id] =
                        localCustomer;
                }

                await MarkSyncSucceededAsync(
                    tenantId,
                    now,
                    cancellationToken);

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
                "Customer synchronization failed for tenant {TenantId}.",
                tenantId);

            throw;
        }
    }

    public async Task UpsertFromServerAsync(
        CustomerResult serverCustomer,
        Guid? originatingLocalId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateServerCustomer(serverCustomer);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var now =
            DateTime.UtcNow;

        LocalCustomer? localCustomer =
            null;

        if (originatingLocalId.HasValue &&
            originatingLocalId.Value != Guid.Empty)
        {
            localCustomer =
                await _db.Customers
                    .FirstOrDefaultAsync(
                        customer =>
                            customer.TenantId == tenantId &&
                            customer.Id ==
                            originatingLocalId.Value,
                        cancellationToken);
        }

        localCustomer ??=
            await _db.Customers
                .FirstOrDefaultAsync(
                    customer =>
                        customer.TenantId == tenantId &&
                        customer.ServerId ==
                        serverCustomer.Id,
                    cancellationToken);

        if (localCustomer == null)
        {
            localCustomer =
                new LocalCustomer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CreatedAtUtc = now
                };

            _db.Customers.Add(localCustomer);
        }

        ApplyServerCustomer(
            localCustomer,
            serverCustomer,
            tenantId,
            now,
            preserveCurrentBalance: false);

        if (originatingLocalId.HasValue)
        {
            await CompleteQueueItemsAsync(
                tenantId,
                localCustomer.Id,
                now,
                cancellationToken);
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task MarkDeletedFromServerAsync(
        Guid serverCustomerId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var localCustomer =
            await _db.Customers
                .FirstOrDefaultAsync(
                    customer =>
                        customer.TenantId == tenantId &&
                        customer.ServerId ==
                        serverCustomerId,
                    cancellationToken);

        if (localCustomer == null)
            return;

        var now =
            DateTime.UtcNow;

        localCustomer.IsDeleted =
            true;

        localCustomer.IsActive =
            false;

        localCustomer.DeletedAtUtc =
            now;

        localCustomer.ModifiedAtUtc =
            now;

        localCustomer.LastSyncedAtUtc =
            now;

        localCustomer.SyncStatus =
            SyncQueueStatus.Done;

        await CompleteQueueItemsAsync(
            tenantId,
            localCustomer.Id,
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
                    state.EntityName == CustomerEntityName &&
                    state.InitialSyncCompleted,
                cancellationToken);
    }

    private async Task<List<CustomerResult>>
        DownloadAllCustomersAsync(
            CancellationToken cancellationToken)
    {
        var customers =
            new List<CustomerResult>();

        for (var page = 1;
             page <= MaximumPages;
             page++)
        {
            var response =
                await _customerApi.Search(
                    new CustomerQuery
                    {
                        Page = page,
                        PageSize = PageSize,
                        SortBy = "name",
                        Desc = false
                    },
                    cancellationToken);

            var pageItems =
                response.Items?.ToList()
                ?? new List<CustomerResult>();

            if (pageItems.Count == 0)
                break;

            customers.AddRange(pageItems);

            if (response.TotalCount > 0 &&
                customers.Count >= response.TotalCount)
            {
                break;
            }

            if (pageItems.Count < PageSize)
                break;
        }

        return customers
            .Where(customer =>
                customer.Id != Guid.Empty)
            .GroupBy(customer =>
                customer.Id)
            .Select(group =>
                group.Last())
            .ToList();
    }

    private static void ApplyServerCustomer(
        LocalCustomer localCustomer,
        CustomerResult serverCustomer,
        Guid tenantId,
        DateTime now,
        bool preserveCurrentBalance)
    {
        localCustomer.TenantId =
            tenantId;

        localCustomer.ServerId =
            serverCustomer.Id;

        localCustomer.Name =
            serverCustomer.Name.Trim();

        localCustomer.Email =
            NormalizeNullable(serverCustomer.Email);

        localCustomer.Phone =
            NormalizeNullable(serverCustomer.Phone);

        localCustomer.Address =
            NormalizeNullable(serverCustomer.Address);

        localCustomer.TaxNumber =
            NormalizeNullable(serverCustomer.TaxNumber);

        localCustomer.CreditLimit =
            serverCustomer.CreditLimit;

        if (!preserveCurrentBalance)
        {
            localCustomer.CurrentBalance =
                serverCustomer.CurrentBalance;
        }

        localCustomer.IsActive =
            serverCustomer.IsActive;

        localCustomer.Notes =
            NormalizeNullable(serverCustomer.Notes);

        localCustomer.IsDeleted =
            false;

        localCustomer.DeletedAtUtc =
            null;

        localCustomer.SyncStatus =
            SyncQueueStatus.Done;

        localCustomer.ModifiedAtUtc =
            now;

        localCustomer.LastSyncedAtUtc =
            now;

        if (localCustomer.CreatedAtUtc == default)
        {
            localCustomer.CreatedAtUtc =
                now;
        }
    }

    private async Task CompleteQueueItemsAsync(
        Guid tenantId,
        Guid localCustomerId,
        DateTime processedAtUtc,
        CancellationToken cancellationToken)
    {
        var queueItems =
            await _db.SyncQueueItems
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.EntityName == CustomerEntityName &&
                    item.LocalEntityId == localCustomerId &&
                    item.Status != SyncQueueStatus.Done)
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
                        CustomerEntityName,
                    cancellationToken);

        if (state == null)
        {
            state =
                new SyncTableStateLocal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    EntityName = CustomerEntityName,
                    Syncmode = FullSyncMode
                };

            _db.SyncTableStates.Add(state);
        }

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
                            CustomerEntityName,
                        CancellationToken.None);

            if (state == null)
            {
                state =
                    new SyncTableStateLocal
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        EntityName = CustomerEntityName,
                        Syncmode = FullSyncMode
                    };

                _db.SyncTableStates.Add(state);
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
                "Could not save Customer synchronization failure.");
        }
    }

    private static void ValidateServerCustomer(
        CustomerResult customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (customer.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The server returned a Customer without an Id.");
        }

        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            throw new InvalidOperationException(
                $"Server Customer '{customer.Id}' has no name.");
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
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}