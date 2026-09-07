using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
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
            ArgumentNullException.ThrowIfNull(
                db);

            ArgumentNullException.ThrowIfNull(
                api);

            ArgumentNullException.ThrowIfNull(
                tenantContext);

            _db =
                db;

            _api =
                api;

            _tenantContext =
                tenantContext;
        }

        public async Task<LocalCustomerTransactionUploadResult>
            UploadPendingAsync(
                CancellationToken cancellationToken = default)
        {
            var result =
                new LocalCustomerTransactionUploadResult();

            var lockAcquired =
                await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                    0,
                    cancellationToken);

            if (!lockAcquired)
            {
                result.Skipped++;

                result.Messages.Add(
                    "A local database operation is already running.");

                return result;
            }

            try
            {
                _db.ChangeTracker.Clear();

                var uploadResult =
                    await UploadPendingCoreAsync(
                        result,
                        cancellationToken);

                _db.ChangeTracker.Clear();

                return uploadResult;
            }
            catch (OperationCanceledException)
                when (
                    cancellationToken
                        .IsCancellationRequested)
            {
                _db.ChangeTracker.Clear();

                throw;
            }
            catch
            {
                _db.ChangeTracker.Clear();

                throw;
            }
            finally
            {
                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        private async Task<LocalCustomerTransactionUploadResult>
            UploadPendingCoreAsync(
                LocalCustomerTransactionUploadResult result,
                CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                result);

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            var queueItems =
                await _db.SyncQueueItems
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            item.EntityName ==
                                CustomerTransactionEntityName &&
                            (
                                item.Status ==
                                    SyncQueueStatus.Pending ||
                                item.Status ==
                                    SyncQueueStatus.Processing
                            ))
                    .OrderBy(
                        item =>
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

                    queueItem.ProcessedAtUtc =
                        null;

                    queueItem.ErrorMessage =
                        null;

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    var localTransaction =
                        await _db.CustomerTransactions
                            .FirstOrDefaultAsync(
                                transaction =>
                                    transaction.TenantId ==
                                        tenantId &&
                                    transaction.Id ==
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

                    if (localTransaction.ClientOperationId ==
                            Guid.Empty ||
                        queueItem.ClientOperationId ==
                            Guid.Empty ||
                        localTransaction.ClientOperationId !=
                            queueItem.ClientOperationId)
                    {
                        MarkFailed(
                            queueItem,
                            result,
                            $"Customer transaction " +
                            $"'{localTransaction.Id}' has an invalid " +
                            "ClientOperationId.");

                        await _db.SaveChangesAsync(
                            cancellationToken);

                        continue;
                    }

                    /*
                     * Credit transactions generated by sales are
                     * uploaded through the authoritative Sale endpoint.
                     */
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
                        if (!localTransaction
                                .LocalCashSessionId
                                .HasValue)
                        {
                            MarkFailed(
                                queueItem,
                                result,
                                "Cash transaction has no local " +
                                "cash session.");

                            await _db.SaveChangesAsync(
                                cancellationToken);

                            continue;
                        }

                        var cashSession =
                            await _db.CashSessions
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    session =>
                                        session.TenantId ==
                                            tenantId &&
                                        session.Id ==
                                            localTransaction
                                                .LocalCashSessionId
                                                .Value,
                                    cancellationToken);

                        if (cashSession?.ServerId == null ||
                            cashSession.ServerId ==
                                Guid.Empty)
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
                            cashSession.ServerId.Value;
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
                                        localTransaction
                                            .ClientOperationId,

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
                                        localTransaction
                                            .TransactionDateUtc
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
                                            localTransaction
                                                .ClientOperationId,

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
                                            localTransaction
                                                .TransactionDateUtc
                                    },
                                    cancellationToken)

                                : throw new InvalidOperationException(
                                    $"Unsupported standalone customer " +
                                    $"transaction type " +
                                    $"'{localTransaction.Type}'.");

                    ValidateServerResult(
                        serverResult.Id,
                        serverResult.ClientOperationId,
                        serverResult.CustomerId,
                        serverResult.Type,
                        serverResult.Amount,
                        serverResult.IsCash,
                        serverResult.CashSessionId,
                        localTransaction,
                        queueItem,
                        customer.ServerId.Value,
                        serverCashSessionId);

                    var now =
                        DateTime.UtcNow;

                    /*
                     * Validate and reconcile related movements before
                     * marking the customer transaction as synchronized.
                     */
                    await MarkRelatedCashMovementDoneAsync(
                        tenantId,
                        localTransaction.ClientOperationId,
                        serverResult.Id,
                        serverCashSessionId,
                        now,
                        cancellationToken);

                    /*
                     * Historical recovery:
                     * adjust a session already closed and synchronized
                     * before this payment or refund was uploaded.
                     */
                    await ReconcileClosedLocalCashSessionAsync(
                        tenantId,
                        localTransaction,
                        now,
                        cancellationToken);

                    localTransaction.ServerId =
                        serverResult.Id;

                    localTransaction.CustomerServerId =
                        customer.ServerId.Value;

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

                    MarkDone(
                        localTransaction,
                        queueItem,
                        now);

                    result.Synced++;

                    result.Messages.Add(
                        $"{localTransaction.Type} for customer " +
                        $"'{customer.Name}' synchronized.");

                    /*
                     * Transaction, customer, movement, cash session
                     * and queue item are persisted together.
                     */
                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (
                        cancellationToken
                            .IsCancellationRequested)
                {
                    throw;
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

                        queueItem.ProcessedAtUtc =
                            null;
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

                    queueItem.ProcessedAtUtc =
                        null;

                    queueItem.ErrorMessage =
                        exception.Message;

                    result.Failed++;

                    result.Messages.Add(
                        "Customer transaction upload stopped: " +
                        "API offline.");

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    break;
                }
                catch (Exception exception)
                {
                    /*
                     * Discard every tracked mutation made after the
                     * server response. Only the failed queue status
                     * is persisted.
                     */
                    var queueItemId =
                        queueItem.Id;

                    var errorMessage =
                        exception
                            .GetBaseException()
                            .Message;

                    _db.ChangeTracker.Clear();

                    var persistedQueueItem =
                        await _db.SyncQueueItems
                            .FirstOrDefaultAsync(
                                item =>
                                    item.TenantId == tenantId &&
                                    item.Id == queueItemId,
                                cancellationToken);

                    if (persistedQueueItem != null)
                    {
                        persistedQueueItem.Status =
                            SyncQueueStatus.Failed;

                        persistedQueueItem.ErrorMessage =
                            errorMessage;

                        await _db.SaveChangesAsync(
                            cancellationToken);
                    }

                    result.Failed++;

                    result.Messages.Add(
                        errorMessage);
                }
            }

            return result;
        }

        private static void ValidateServerResult(
            Guid serverTransactionId,
            Guid serverClientOperationId,
            Guid serverCustomerId,
            string serverType,
            decimal serverAmount,
            bool serverIsCash,
            Guid? returnedServerCashSessionId,
            LocalCustomerTransaction localTransaction,
            SyncQueueItem queueItem,
            Guid expectedCustomerId,
            Guid? expectedServerCashSessionId)
        {
            if (serverTransactionId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The server returned no customer " +
                    "transaction identifier.");
            }

            if (serverClientOperationId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The server returned no client operation " +
                    "identifier.");
            }

            if (serverClientOperationId !=
                localTransaction.ClientOperationId)
            {
                throw new InvalidOperationException(
                    $"The server returned client operation " +
                    $"'{serverClientOperationId}', but local operation " +
                    $"'{localTransaction.ClientOperationId}' " +
                    "was expected.");
            }

            if (serverCustomerId !=
                expectedCustomerId)
            {
                throw new InvalidOperationException(
                    $"The server returned customer " +
                    $"'{serverCustomerId}', but customer " +
                    $"'{expectedCustomerId}' was expected.");
            }

            if (!string.Equals(
                    serverType,
                    localTransaction.Type,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The server returned transaction type " +
                    $"'{serverType}', but type " +
                    $"'{localTransaction.Type}' was expected.");
            }

            if (serverIsCash !=
                localTransaction.IsCash)
            {
                throw new InvalidOperationException(
                    "The server returned a different cash-payment state.");
            }

            if (returnedServerCashSessionId !=
                expectedServerCashSessionId)
            {
                throw new InvalidOperationException(
                    $"The server returned cash session " +
                    $"'{returnedServerCashSessionId}', but session " +
                    $"'{expectedServerCashSessionId}' was expected.");
            }

            var localAmount =
                Math.Round(
                    localTransaction.Amount,
                    2,
                    MidpointRounding.AwayFromZero);

            var normalizedServerAmount =
                Math.Round(
                    serverAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            if (normalizedServerAmount !=
                localAmount)
            {
                throw new InvalidOperationException(
                    $"The server returned amount " +
                    $"'{normalizedServerAmount:0.00}', but amount " +
                    $"'{localAmount:0.00}' was expected.");
            }

            if (localTransaction.ServerId.HasValue &&
                localTransaction.ServerId.Value !=
                    Guid.Empty &&
                localTransaction.ServerId.Value !=
                    serverTransactionId)
            {
                throw new InvalidOperationException(
                    $"Local customer transaction " +
                    $"'{localTransaction.Id}' is already linked " +
                    "to another server transaction.");
            }

            if (queueItem.ServerEntityId.HasValue &&
                queueItem.ServerEntityId.Value !=
                    Guid.Empty &&
                queueItem.ServerEntityId.Value !=
                    serverTransactionId)
            {
                throw new InvalidOperationException(
                    $"Queue item '{queueItem.Id}' is already linked " +
                    "to another server transaction.");
            }
        }

        private async Task ReconcileClosedLocalCashSessionAsync(
            Guid tenantId,
            LocalCustomerTransaction transaction,
            DateTime synchronizedAtUtc,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                transaction);

            if (!transaction.IsCash ||
                !transaction.LocalCashSessionId.HasValue)
            {
                return;
            }

            /*
             * Prevent a second local adjustment if an inconsistent
             * queue row points to an already completed transaction.
             */
            if (transaction.SyncStatus ==
                SyncQueueStatus.Done)
            {
                return;
            }

            var cashSession =
                await _db.CashSessions
                    .FirstOrDefaultAsync(
                        session =>
                            session.TenantId == tenantId &&
                            session.Id ==
                                transaction.LocalCashSessionId.Value,
                        cancellationToken);

            if (cashSession == null)
            {
                throw new InvalidOperationException(
                    $"Local cash session " +
                    $"'{transaction.LocalCashSessionId.Value}' " +
                    "was not found.");
            }

            /*
             * Closed/Pending sessions still execute the normal
             * server closure phase. Their totals must not be changed
             * here.
             *
             * Closed/Done is the historical inconsistent state where
             * closure completed before this operation was uploaded.
             */
            if (cashSession.Status !=
                    LocalCashSessionStatus.Closed ||
                cashSession.SyncStatus !=
                    SyncQueueStatus.Done)
            {
                return;
            }

            var amount =
                Math.Round(
                    transaction.Amount,
                    2,
                    MidpointRounding.AwayFromZero);

            decimal signedAmount;

            if (string.Equals(
                    transaction.Type,
                    LocalCustomerTransactionType.Payment,
                    StringComparison.OrdinalIgnoreCase))
            {
                signedAmount =
                    amount;
            }
            else if (string.Equals(
                         transaction.Type,
                         LocalCustomerTransactionType.Refund,
                         StringComparison.OrdinalIgnoreCase))
            {
                signedAmount =
                    -amount;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported customer transaction type " +
                    $"'{transaction.Type}' for cash-session " +
                    "reconciliation.");
            }

            cashSession.ClosingAmountExpected =
                Math.Round(
                    cashSession.ClosingAmountExpected +
                    signedAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            cashSession.Difference =
                Math.Round(
                    cashSession.ClosingAmountCounted -
                    cashSession.ClosingAmountExpected,
                    2,
                    MidpointRounding.AwayFromZero);

            cashSession.LastSyncedAtUtc =
                synchronizedAtUtc;
        }

        private async Task MarkRelatedCashMovementDoneAsync(
            Guid tenantId,
            Guid clientOperationId,
            Guid serverTransactionId,
            Guid? serverCashSessionId,
            DateTime synchronizedAtUtc,
            CancellationToken cancellationToken)
        {
            var movements =
                await _db.CashMovements
                    .Where(
                        movement =>
                            movement.TenantId == tenantId &&
                            movement.LocalReferenceId ==
                                clientOperationId &&
                            movement.SyncStatus !=
                                SyncQueueStatus.Done)
                    .ToListAsync(
                        cancellationToken);

            if (serverCashSessionId.HasValue &&
                movements.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Cash customer transaction " +
                    $"'{clientOperationId}' must have exactly one " +
                    $"pending local cash movement. Found " +
                    $"{movements.Count}.");
            }

            if (!serverCashSessionId.HasValue &&
                movements.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Non-cash customer transaction " +
                    $"'{clientOperationId}' has an unexpected " +
                    "local cash movement.");
            }

            /*
             * Validate every movement before changing any movement.
             */
            foreach (var movement in movements)
            {
                if (movement.ServerReferenceId.HasValue &&
                    movement.ServerReferenceId.Value !=
                        Guid.Empty &&
                    movement.ServerReferenceId.Value !=
                        serverTransactionId)
                {
                    throw new InvalidOperationException(
                        $"Cash movement '{movement.Id}' is already " +
                        "linked to another server transaction.");
                }
            }

            foreach (var movement in movements)
            {
                movement.ServerReferenceId =
                    serverTransactionId;

                if (serverCashSessionId.HasValue &&
                    serverCashSessionId.Value != Guid.Empty)
                {
                    movement.ServerCashSessionId =
                        serverCashSessionId.Value;
                }

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

            queueItem.ServerEntityId =
                transaction.ServerId;

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

            queueItem.ProcessedAtUtc =
                null;

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

            queueItem.ProcessedAtUtc =
                DateTime.UtcNow;

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