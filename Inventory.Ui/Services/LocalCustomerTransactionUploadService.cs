using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Results;
using Inventory.Ui.Interfaces;
using Microsoft.EntityFrameworkCore;
using Refit;

namespace Inventory.Ui.Services
{
    public sealed class LocalCustomerTransactionUploadService
        : ILocalCustomerTransactionUploadService
    {
        private const string CustomerTransactionEntityName =
            "CustomerTransaction";

        private readonly PosLocalDbContext _db;
        private readonly ICustomerTransactionsApi _api;
        private readonly ILocalTenantContext _tenantContext;

        public LocalCustomerTransactionUploadService(
            PosLocalDbContext db,
            ICustomerTransactionsApi api,
            ILocalTenantContext tenantContext)
        {
            _db = db;
            _api = api;
            _tenantContext = tenantContext;
        }

        public async Task<LocalCustomerTransactionUploadResult>
            UploadPendingAsync(
                CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            var result =
                new LocalCustomerTransactionUploadResult();

            var queueItems =
                await _db.SyncQueueItems
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.EntityName ==
                            CustomerTransactionEntityName &&
                        item.Status !=
                            SyncQueueStatus.Done)
                    .OrderBy(item =>
                        item.CreatedAtUtc)
                    .Take(100)
                    .ToListAsync(
                        cancellationToken);

            result.TotalPending =
                queueItems.Count;

            foreach (var queueItem in queueItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    queueItem.Status =
                        SyncQueueStatus.Processing;

                    queueItem.Attempts++;

                    queueItem.ErrorMessage =
                        null;

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    var localTransaction =
                        await _db.CustomerTransactions
                            .FirstOrDefaultAsync(
                                item =>
                                    item.TenantId == tenantId &&
                                    item.Id ==
                                        queueItem.LocalEntityId,
                                cancellationToken);

                    if (localTransaction == null)
                    {
                        MarkFailed(
                            queueItem,
                            result,
                            "Local customer transaction was not found.");

                        await _db.SaveChangesAsync(
                            cancellationToken);

                        continue;
                    }

                    if (!localTransaction.UploadRequired)
                    {
                        MarkDone(
                            localTransaction,
                            queueItem,
                            DateTime.UtcNow);

                        result.Skipped++;

                        result.Messages.Add(
                            $"Local {localTransaction.Origin} ledger row " +
                            "is uploaded by its authoritative endpoint.");

                        await _db.SaveChangesAsync(
                            cancellationToken);

                        continue;
                    }

                    var customer =
                        await _db.Customers
                            .FirstOrDefaultAsync(
                                item =>
                                    item.TenantId == tenantId &&
                                    item.Id ==
                                        localTransaction.CustomerLocalId &&
                                    !item.IsDeleted,
                                cancellationToken);

                    if (customer?.ServerId == null ||
                        customer.ServerId == Guid.Empty)
                    {
                        MarkPending(
                            queueItem,
                            result,
                            "Customer has no ServerId yet.");

                        await _db.SaveChangesAsync(
                            cancellationToken);

                        continue;
                    }

                    Guid? serverCashSessionId =
                        null;

                    if (localTransaction.IsCash)
                    {
                        if (!localTransaction.LocalCashSessionId.HasValue)
                        {
                            MarkFailed(
                                queueItem,
                                result,
                                "Cash transaction has no local cash session.");

                            await _db.SaveChangesAsync(
                                cancellationToken);

                            continue;
                        }

                        var cashSession =
                            await _db.CashSessions
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    session =>
                                        session.TenantId == tenantId &&
                                        session.Id ==
                                            localTransaction
                                                .LocalCashSessionId.Value,
                                    cancellationToken);

                        if (cashSession?.ServerId == null ||
                            cashSession.ServerId == Guid.Empty)
                        {
                            MarkPending(
                                queueItem,
                                result,
                                "Cash session has no ServerId yet.");

                            await _db.SaveChangesAsync(
                                cancellationToken);

                            continue;
                        }

                        serverCashSessionId =
                            cashSession.ServerId;
                    }

                    var serverResult =
                        string.Equals(
                            localTransaction.Type,
                            LocalCustomerTransactionType.Payment,
                            StringComparison.OrdinalIgnoreCase)

                            ? await _api.RegisterPayment(
                                new RegisterCustomerPaymentRequest
                                {
                                    ClientOperationId =
                                        localTransaction.ClientOperationId,

                                    CustomerId =
                                        customer.ServerId.Value,

                                    Amount =
                                        localTransaction.Amount,

                                    Description =
                                        localTransaction.Description,

                                    IsCash =
                                        localTransaction.IsCash,

                                    CashSessionId =
                                        serverCashSessionId,

                                    TransactionDateUtc =
                                        localTransaction.TransactionDateUtc
                                },
                                cancellationToken)

                            : string.Equals(
                                localTransaction.Type,
                                LocalCustomerTransactionType.Refund,
                                StringComparison.OrdinalIgnoreCase)

                                ? await _api.RegisterRefund(
                                    new RegisterCustomerRefundRequest
                                    {
                                        ClientOperationId =
                                            localTransaction.ClientOperationId,

                                        CustomerId =
                                            customer.ServerId.Value,

                                        Amount =
                                            localTransaction.Amount,

                                        Description =
                                            localTransaction.Description,

                                        IsCash =
                                            localTransaction.IsCash,

                                        CashSessionId =
                                            serverCashSessionId,

                                        TransactionDateUtc =
                                            localTransaction.TransactionDateUtc
                                    },
                                    cancellationToken)

                                : throw new InvalidOperationException(
                                    $"Unsupported standalone customer " +
                                    $"transaction type " +
                                    $"'{localTransaction.Type}'.");

                    var now =
                        DateTime.UtcNow;

                    localTransaction.ServerId =
                        serverResult.Id;

                    localTransaction.CustomerServerId =
                        customer.ServerId;

                    localTransaction.ServerCashSessionId =
                        serverCashSessionId;

                    localTransaction.BalanceBefore =
                        serverResult.BalanceBefore;

                    localTransaction.BalanceAfter =
                        serverResult.BalanceAfter;

                    customer.CurrentBalance =
                        serverResult.BalanceAfter;

                    customer.ModifiedAtUtc =
                        now;

                    await MarkRelatedCashMovementDoneAsync(
                        tenantId,
                        localTransaction.ClientOperationId,
                        now,
                        cancellationToken);

                    MarkDone(
                        localTransaction,
                        queueItem,
                        now);

                    result.Synced++;

                    result.Messages.Add(
                        $"{localTransaction.Type} for customer " +
                        $"'{customer.Name}' synchronized.");

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (ApiException exception)
                {
                    queueItem.ErrorMessage =
                        exception.Content ??
                        exception.Message;

                    if (IsTemporaryApiError(
                            exception))
                    {
                        queueItem.Status =
                            SyncQueueStatus.Pending;
                    }
                    else
                    {
                        queueItem.Status =
                            SyncQueueStatus.Conflict;
                    }

                    result.Failed++;

                    result.Messages.Add(
                        queueItem.ErrorMessage);

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (HttpRequestException exception)
                {
                    queueItem.Status =
                        SyncQueueStatus.Pending;

                    queueItem.ErrorMessage =
                        exception.Message;

                    result.Failed++;

                    result.Messages.Add(
                        "Customer transaction upload stopped: API offline.");

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    break;
                }
                catch (Exception exception)
                {
                    queueItem.Status =
                        SyncQueueStatus.Failed;

                    queueItem.ErrorMessage =
                        exception.GetBaseException().Message;

                    result.Failed++;

                    result.Messages.Add(
                        queueItem.ErrorMessage);

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
            }

            return result;
        }

        private async Task MarkRelatedCashMovementDoneAsync(
            Guid tenantId,
            Guid clientOperationId,
            DateTime synchronizedAtUtc,
            CancellationToken cancellationToken)
        {
            var movements =
                await _db.CashMovements
                    .Where(movement =>
                        movement.TenantId == tenantId &&
                        movement.LocalReferenceId ==
                            clientOperationId &&
                        movement.SyncStatus !=
                            SyncQueueStatus.Done)
                    .ToListAsync(
                        cancellationToken);

            foreach (var movement in movements)
            {
                movement.SyncStatus =
                    SyncQueueStatus.Done;

                movement.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }
        }

        private static void MarkDone(
            LocalCustomerTransaction transaction,
            SyncQueueItem queueItem,
            DateTime synchronizedAtUtc)
        {
            transaction.SyncStatus =
                SyncQueueStatus.Done;

            transaction.LastSyncedAtUtc =
                synchronizedAtUtc;

            queueItem.Status =
                SyncQueueStatus.Done;

            queueItem.ProcessedAtUtc =
                synchronizedAtUtc;

            queueItem.ErrorMessage =
                null;
        }

        private static void MarkPending(
            SyncQueueItem queueItem,
            LocalCustomerTransactionUploadResult result,
            string message)
        {
            queueItem.Status =
                SyncQueueStatus.Pending;

            queueItem.ErrorMessage =
                message;

            result.Skipped++;

            result.Messages.Add(
                message);
        }

        private static void MarkFailed(
            SyncQueueItem queueItem,
            LocalCustomerTransactionUploadResult result,
            string message)
        {
            queueItem.Status =
                SyncQueueStatus.Failed;

            queueItem.ErrorMessage =
                message;

            result.Failed++;

            result.Messages.Add(
                message);
        }

        private static bool IsTemporaryApiError(
            ApiException exception)
        {
            return (int)exception.StatusCode >= 500 ||
                   exception.StatusCode ==
                       System.Net.HttpStatusCode.RequestTimeout ||
                   exception.StatusCode ==
                       System.Net.HttpStatusCode.TooManyRequests ||
                   exception.StatusCode ==
                       System.Net.HttpStatusCode.Unauthorized ||
                   exception.StatusCode ==
                       System.Net.HttpStatusCode.Forbidden;
        }
    }
}
