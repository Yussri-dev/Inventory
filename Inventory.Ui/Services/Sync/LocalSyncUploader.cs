using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Suppliers.Requests;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Refit;
using System.Reflection;

namespace Inventory.Ui.Services.Sync
{
    public sealed class LocalSyncUploader : ILocalSyncUploader
    {
        private const string CustomerEntityName = "Customer";
        private const string SupplierEntityName = "Supplier";
        private const string ProductEntityName = "Product";
        private const string DamageEntityName = "Damage";
        private const string PurchaseEntityName = "Purchase";

        private readonly PosLocalDbContext _db;
        private readonly ISaleApi _saleApi;
        private readonly IPurchaseApi _purchaseApi;
        private readonly ICustomerApi _customerApi;
        private readonly ISupplierApi _supplierApi;
        private readonly ICashSessionApi _cashSessionApi;
        private readonly IProductApi _productApi;
        private readonly IDamageApi _damageApi;
        private readonly ILocalDamageSyncService _damageSync;
        private readonly ILocalTenantContext _tenantContext;
        private readonly ILogger<LocalSyncUploader> _logger;

        private Guid CurrentTenantId =>
            _tenantContext.GetRequiredTenantId();

        public LocalSyncUploader(
            PosLocalDbContext db,
            ISaleApi saleApi,
            IPurchaseApi purchaseApi,
            ICustomerApi customerApi,
            ISupplierApi supplierApi,
            IProductApi productApi,
            ICashSessionApi cashSessionApi,
            IDamageApi damageApi,
            ILocalDamageSyncService damageSync,
            ILocalTenantContext tenantContext,
            ILogger<LocalSyncUploader> logger)
        {
            _db = db;
            _saleApi = saleApi;
            _purchaseApi = purchaseApi;
            _customerApi = customerApi;
            _supplierApi = supplierApi;
            _productApi = productApi;
            _cashSessionApi = cashSessionApi;
            _damageApi = damageApi;
            _damageSync = damageSync;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<LocalSyncUploadResult> SyncPendingAsync(
    CancellationToken cancellationToken = default)
        {
            var result =
                new LocalSyncUploadResult();

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
                var tenantId =
                    CurrentTenantId;

                /*
                 * The context may have retained entities from an older
                 * operation. Synchronization starts with a clean tracker.
                 */
                _db.ChangeTracker.Clear();

                await RecoverInterruptedQueueItemsAsync(
                    tenantId,
                    cancellationToken);

                _logger.LogInformation(
     "Synchronization started for tenant {TenantId}.",
     tenantId);

                _logger.LogInformation(
                    "Synchronizing cash sessions.");

                await SyncCashSessionsCreateAsync(
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Synchronizing customers.");

                await SyncCustomersAsync(
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Synchronizing suppliers.");

                await SyncSuppliersAsync(
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Synchronizing products.");

                await SyncProductsAsync(
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Synchronizing purchases.");

                await SyncPurchasesAsync(
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Synchronizing sales.");

                await SyncSalesAsync(
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Synchronizing damages.");

                await SyncDamagesAsync(
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Synchronizing closed cash sessions.");

                await SyncClosedCashSessionsAsync(
                    result,
                    cancellationToken);

                _logger.LogInformation(
                    "Synchronization completed. Synced={Synced}, " +
                    "Failed={Failed}, Skipped={Skipped}.",
                    result.Synced,
                    result.Failed,
                    result.Skipped);

                _db.ChangeTracker.Clear();
            }
            catch (OperationCanceledException)
            {
                _db.ChangeTracker.Clear();

                throw;
            }
            catch (HttpRequestException exception)
            {
                _db.ChangeTracker.Clear();

                result.Failed++;

                result.Messages.Add(
                    "API offline. Synchronization stopped.");

                _logger.LogWarning(
                    exception,
                    "Synchronization stopped because the API is unavailable.");
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

            return result;
        }

        private async Task RecoverInterruptedQueueItemsAsync(
    Guid tenantId,
    CancellationToken cancellationToken)
        {
            var interruptedItems =
                await _db.SyncQueueItems
                    .Where(queueItem =>
                        queueItem.TenantId == tenantId &&
                        queueItem.Status ==
                            SyncQueueStatus.Processing)
                    .ToListAsync(
                        cancellationToken);

            if (interruptedItems.Count == 0)
            {
                return;
            }

            foreach (var queueItem in interruptedItems)
            {
                queueItem.Status =
                    SyncQueueStatus.Pending;

                queueItem.ErrorMessage =
                    "Recovered after an interrupted synchronization.";

                queueItem.ProcessedAtUtc =
                    null;
            }

            await _db.SaveChangesAsync(
                cancellationToken);

            _logger.LogWarning(
                "Recovered {Count} interrupted synchronization items.",
                interruptedItems.Count);
        }

        // ============================================================
        // CASH SESSIONS
        // ============================================================
        private async Task SyncCashSessionsCreateAsync(
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            var sessions =
                await _db.CashSessions
                    .Where(session =>
                        session.TenantId ==
                            CurrentTenantId &&
                        session.ServerId ==
                            null)
                    .OrderBy(session =>
                        session.OpenedAtUtc)
                    .ToListAsync(
                        cancellationToken);

            result.TotalPending +=
                sessions.Count;

            foreach (var session in sessions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (session.ClientOperationId ==
                        Guid.Empty)
                    {
                        /*
                         * Legacy local row. Assign the id once and persist
                         * it before the HTTP request so every retry uses
                         * the same value.
                         */
                        session.ClientOperationId =
                            Guid.NewGuid();
                    }

                    session.SyncStatus =
                        SyncQueueStatus.Processing;

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    var request =
                        new CreateCashSessionRequest
                        {
                            ClientOperationId =
                                session.ClientOperationId,

                            OpenedAtUtc =
                                EnsureUtc(
                                    session.OpenedAtUtc),

                            OpeningAmount =
                                session.OpeningAmount,

                            OpeningNotes =
                                BuildOpeningNotes(
                                    session)
                        };

                    /*
                     * The server is idempotent by
                     * TenantId + ClientOperationId:
                     *
                     * - first request creates the session;
                     * - retry returns the same session;
                     * - another open operation returns HTTP 409.
                     */
                    var serverSession =
                        await _cashSessionApi.Create(
                            request);

                    var now =
                        DateTime.UtcNow;

                    session.ServerId =
                        serverSession.Id;

                    session.SessionNumber =
                        string.IsNullOrWhiteSpace(
                            serverSession.SessionNumber)
                            ? session.SessionNumber
                            : serverSession.SessionNumber;

                    session.LastSyncedAtUtc =
                        now;

                    /*
                     * A locally closed session still needs the server
                     * close call after its dependent sales are uploaded.
                     */
                    session.SyncStatus =
                        session.Status ==
                            LocalCashSessionStatus.Closed
                            ? SyncQueueStatus.Pending
                            : SyncQueueStatus.Done;

                    var sales =
                        await _db.Sales
                            .Where(sale =>
                                sale.TenantId ==
                                    CurrentTenantId &&
                                sale.LocalCashSessionId ==
                                    session.Id)
                            .ToListAsync(
                                cancellationToken);

                    foreach (var sale in sales)
                    {
                        sale.CashSessionServerId =
                            serverSession.Id;
                    }

                    var movements =
                        await _db.CashMovements
                            .Where(movement =>
                                movement.TenantId ==
                                    CurrentTenantId &&
                                movement.LocalCashSessionId ==
                                    session.Id)
                            .ToListAsync(
                                cancellationToken);

                    foreach (var movement in movements)
                    {
                        movement.ServerCashSessionId =
                            serverSession.Id;

                        if (movement.Type ==
                            LocalCashMovementType.Opening)
                        {
                            movement.SyncStatus =
                                SyncQueueStatus.Done;

                            movement.LastSyncedAtUtc =
                                now;
                        }
                    }

                    result.Synced++;

                    result.Messages.Add(
                        $"Cash session {session.SessionNumber} " +
                        "linked to the server successfully.");

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (HttpRequestException)
                {
                    session.SyncStatus =
                        SyncQueueStatus.Pending;

                    result.Messages.Add(
                        $"API offline while creating cash session " +
                        $"{session.SessionNumber}. The same " +
                        "ClientOperationId will be retried.");

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    throw;
                }
                catch (ApiException exception)
                {
                    var message =
                        exception.Content ??
                        exception.Message;

                    if (exception.StatusCode ==
                        System.Net.HttpStatusCode.Conflict)
                    {
                        /*
                         * After the server idempotency fix, 409 means a
                         * DIFFERENT server session is open. Never attach
                         * its ServerId to this local session.
                         */
                        session.SyncStatus =
                            SyncQueueStatus.Conflict;

                        result.Failed++;

                        result.Messages.Add(
                            $"Cash session conflict for " +
                            $"{session.SessionNumber}: {message} " +
                            "Close the different server session, then " +
                            "run synchronization again.");
                    }
                    else if (IsAuthError(
                                 exception))
                    {
                        session.SyncStatus =
                            SyncQueueStatus.Pending;

                        result.Skipped++;

                        result.Messages.Add(
                            "Cash session synchronization requires a " +
                            "new online login.");
                    }
                    else if (IsTemporaryApiError(
                                 exception))
                    {
                        session.SyncStatus =
                            SyncQueueStatus.Pending;

                        result.Failed++;

                        result.Messages.Add(
                            $"Temporary cash-session API error: " +
                            $"{message}");
                    }
                    else
                    {
                        session.SyncStatus =
                            SyncQueueStatus.Failed;

                        result.Failed++;

                        result.Messages.Add(
                            $"Failed creating cash session " +
                            $"{session.SessionNumber}: {message}");
                    }

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
            }
        }

        private static DateTime EnsureUtc(
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

        private async Task SyncClosedCashSessionsAsync(
    LocalSyncUploadResult result,
    CancellationToken cancellationToken)
        {
            var sessions = await _db.CashSessions
                .Where(x =>
                    x.TenantId == CurrentTenantId &&
                    x.ServerId != null &&
                    x.Status == LocalCashSessionStatus.Closed &&
                    x.SyncStatus != SyncQueueStatus.Done)
                .OrderBy(x => x.ClosedAtUtc)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                var hasPendingSales = await _db.Sales
                    .AnyAsync(x =>
                        x.TenantId == CurrentTenantId &&
                        x.LocalCashSessionId == session.Id &&
                        x.SyncStatus != SyncQueueStatus.Done,
                        cancellationToken);

                if (hasPendingSales)
                {
                    result.Skipped++;
                    result.Messages.Add(
                        $"Cash session {session.SessionNumber} skipped: pending sales still exist.");
                    continue;
                }

                try
                {
                    session.SyncStatus = SyncQueueStatus.Processing;
                    await _db.SaveChangesAsync(cancellationToken);

                    var request = new CloseCashSessionRequest
                    {
                        ActualCash = session.ClosingAmountCounted,
                        ClosingNotes = BuildClosingNotes(session)
                    };

                    var serverSession = await _cashSessionApi.Close(
                        session.ServerId!.Value,
                        request);

                    session.ClosingAmountExpected = serverSession.ClosingAmountExpected;
                    session.ClosingAmountCounted = serverSession.ClosingAmountCounted;
                    session.Difference = serverSession.Difference;
                    session.ClosedAtUtc = serverSession.ClosedAt;
                    session.SyncStatus = SyncQueueStatus.Done;
                    session.LastSyncedAtUtc = DateTime.UtcNow;

                    var movements = await _db.CashMovements
                        .Where(x =>
                            x.TenantId == CurrentTenantId &&
                            x.LocalCashSessionId == session.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var movement in movements)
                    {
                        movement.ServerCashSessionId = serverSession.Id;
                        movement.SyncStatus = SyncQueueStatus.Done;
                        movement.LastSyncedAtUtc = DateTime.UtcNow;
                    }

                    result.Synced++;
                    result.Messages.Add($"Cash session {session.SessionNumber} closed online.");

                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (ApiException ex)
                {
                    session.SyncStatus = SyncQueueStatus.Conflict;

                    result.Failed++;
                    result.Messages.Add(
                        $"Failed closing cash session {session.SessionNumber}: {ex.Content ?? ex.Message}");

                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private static string BuildOpeningNotes(LocalCashSession session)
        {
            var note = $"Offline sync | Local session: {session.SessionNumber}";

            if (!string.IsNullOrWhiteSpace(session.OpeningNotes))
                note += $" | {session.OpeningNotes}";

            return note;
        }

        private static string BuildClosingNotes(LocalCashSession session)
        {
            var note = $"Offline close sync | Local session: {session.SessionNumber}";

            if (!string.IsNullOrWhiteSpace(session.ClosingNotes))
                note += $" | {session.ClosingNotes}";

            return note;
        }

        private static bool IsAuthError(ApiException ex)
        {
            return ex.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                   ex.StatusCode == System.Net.HttpStatusCode.Forbidden;
        }

        private static bool IsTemporaryApiError(ApiException ex)
        {
            return (int)ex.StatusCode >= 500 ||
                   ex.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                   ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests;
        }
        // ============================================================
        // CUSTOMERS
        // ============================================================

        #region Customers
        private async Task SyncCustomersAsync(
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            var pendingCustomers = await _db.SyncQueueItems
                .Where(x =>
                    x.TenantId == CurrentTenantId &&
                    x.Status == SyncQueueStatus.Pending &&
                    x.EntityName == CustomerEntityName)
                .OrderBy(x => x.CreatedAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken);

            result.TotalPending += pendingCustomers.Count;

            foreach (var queueItem in pendingCustomers)
            {
                try
                {
                    await SyncCustomerAsync(queueItem, result, cancellationToken);
                }
                catch (HttpRequestException)
                {
                    queueItem.Status = SyncQueueStatus.Pending;
                    queueItem.ErrorMessage = "API offline. Will retry later.";

                    result.Messages.Add("API offline while syncing customers.");

                    await _db.SaveChangesAsync(cancellationToken);
                    throw;
                }
                catch (ApiException ex)
                {
                    queueItem.ErrorMessage = ex.Content ?? ex.Message;

                    if (IsAuthError(ex))
                    {
                        queueItem.Status = SyncQueueStatus.Pending;
                        result.Skipped++;
                        result.Messages.Add($"{queueItem.EntityName} sync skipped: login online again.");
                    }
                    else if (IsTemporaryApiError(ex))
                    {
                        queueItem.Status = SyncQueueStatus.Pending;
                        result.Failed++;
                        result.Messages.Add($"Temporary API error: {queueItem.ErrorMessage}");
                    }
                    else
                    {
                        queueItem.Status = SyncQueueStatus.Conflict;
                        result.Failed++;
                        result.Messages.Add($"Sync conflict: {queueItem.ErrorMessage}");
                    }

                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    queueItem.Status = SyncQueueStatus.Failed;
                    queueItem.ErrorMessage = ex.Message;

                    result.Failed++;
                    result.Messages.Add($"Customer sync failed: {ex.Message}");

                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private async Task SyncCustomerAsync(
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            queueItem.Status = SyncQueueStatus.Processing;
            queueItem.Attempts++;
            queueItem.ErrorMessage = null;

            await _db.SaveChangesAsync(cancellationToken);

            var customer = await _db.Customers
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == CurrentTenantId &&
                        x.Id == queueItem.LocalEntityId,
                    cancellationToken);

            if (customer == null)
            {
                queueItem.Status = SyncQueueStatus.Failed;
                queueItem.ErrorMessage = "Local customer not found.";

                result.Failed++;
                result.Messages.Add($"Local customer not found for queue item {queueItem.Id}.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Create ||
                queueItem.Operation == "Create")
            {
                await SyncCustomerCreateAsync(customer, queueItem, result, cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Update ||
                queueItem.Operation == "Update")
            {
                await SyncCustomerUpdateAsync(customer, queueItem, result, cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Delete ||
                queueItem.Operation == "Delete")
            {
                await SyncCustomerDeleteAsync(customer, queueItem, result, cancellationToken);
                return;
            }

            queueItem.Status = SyncQueueStatus.Failed;
            queueItem.ErrorMessage = $"Unsupported customer operation: {queueItem.Operation}";

            result.Failed++;
            result.Messages.Add(queueItem.ErrorMessage);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncCustomerCreateAsync(
            LocalCustomer customer,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (customer.ServerId.HasValue && customer.ServerId.Value != Guid.Empty)
            {
                customer.SyncStatus = SyncQueueStatus.Done;

                queueItem.Status = SyncQueueStatus.Done;
                queueItem.ProcessedAtUtc = DateTime.UtcNow;
                queueItem.ErrorMessage = null;

                result.Skipped++;
                result.Messages.Add($"Customer {customer.Name} already has ServerId.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var request = new CreateCustomerRequest
            {
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                TaxNumber = customer.TaxNumber,
                CreditLimit = customer.CreditLimit,
                AllowCredit = customer.AllowCredit,
                HasUnlimitedCredit =
                    customer.HasUnlimitedCredit,
                IsActive = customer.IsActive,
                Notes = customer.Notes
            };

            var serverCustomer = await _customerApi.Create(request);

            customer.ServerId = serverCustomer.Id;
            customer.SyncStatus = SyncQueueStatus.Done;
            customer.LastSyncedAtUtc = DateTime.UtcNow;

            await UpdateLocalSalesCustomerServerIdAsync(
                customer.Id,
                serverCustomer.Id,
                cancellationToken);

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Customer {customer.Name} created online.");

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncCustomerUpdateAsync(
            LocalCustomer customer,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (customer.ServerId == null || customer.ServerId == Guid.Empty)
            {
                queueItem.Status = SyncQueueStatus.Pending;
                queueItem.ErrorMessage = "Customer has no ServerId yet.";

                result.Skipped++;
                result.Messages.Add($"Customer {customer.Name} skipped: no ServerId.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var request = new UpdateCustomerRequest
            {
                Id = customer.ServerId.Value,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                TaxNumber = customer.TaxNumber,
                CreditLimit = customer.CreditLimit,
                AllowCredit = customer.AllowCredit,
                HasUnlimitedCredit =
                    customer.HasUnlimitedCredit,
                IsActive = customer.IsActive,
                Notes = customer.Notes
            };

            await _customerApi.Update(customer.ServerId.Value, request);

            customer.SyncStatus = SyncQueueStatus.Done;
            customer.LastSyncedAtUtc = DateTime.UtcNow;

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Customer {customer.Name} updated online.");

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncCustomerDeleteAsync(
            LocalCustomer customer,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (customer.ServerId.HasValue && customer.ServerId.Value != Guid.Empty)
            {
                await _customerApi.Delete(customer.ServerId.Value);
            }

            customer.SyncStatus = SyncQueueStatus.Done;
            customer.LastSyncedAtUtc = DateTime.UtcNow;

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Customer {customer.Name} deleted online.");

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateLocalSalesCustomerServerIdAsync(
            Guid localCustomerId,
            Guid serverCustomerId,
            CancellationToken cancellationToken)
        {
            var sales = await _db.Sales
                .Where(x =>
                    x.TenantId == CurrentTenantId &&
                    x.CustomerServerId == null)
                .ToListAsync(cancellationToken);

            foreach (var sale in sales)
            {
                var customerLocalIdProperty = sale.GetType()
                    .GetProperty("CustomerLocalId", BindingFlags.Public | BindingFlags.Instance);

                if (customerLocalIdProperty == null)
                    continue;

                var value = customerLocalIdProperty.GetValue(sale);

                if (value is Guid customerLocalId && customerLocalId == localCustomerId)
                {
                    sale.CustomerServerId = serverCustomerId;
                }
            }
        }
        #endregion

        // ============================================================
        //SUPPLIERS
        // ============================================================

        #region Suppliers
        private async Task SyncSuppliersAsync(
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            var pendingSuppliers = await _db.SyncQueueItems
                .Where(x =>
                    x.TenantId == CurrentTenantId &&
                    x.Status == SyncQueueStatus.Pending &&
                    x.EntityName == SupplierEntityName)
                .OrderBy(x => x.CreatedAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken);

            result.TotalPending += pendingSuppliers.Count;

            foreach (var queueItem in pendingSuppliers)
            {
                try
                {
                    await SyncSupplierAsync(queueItem, result, cancellationToken);
                }
                catch (HttpRequestException)
                {
                    queueItem.Status = SyncQueueStatus.Pending;
                    queueItem.ErrorMessage = "API offline. Will retry later.";

                    result.Messages.Add("API offline while syncing suppliers.");

                    await _db.SaveChangesAsync(cancellationToken);
                    throw;
                }
                catch (ApiException ex)
                {
                    queueItem.ErrorMessage = ex.Content ?? ex.Message;

                    if (IsAuthError(ex))
                    {
                        queueItem.Status = SyncQueueStatus.Pending;
                        result.Skipped++;
                        result.Messages.Add($"{queueItem.EntityName} sync skipped: login online again.");
                    }
                    else if (IsTemporaryApiError(ex))
                    {
                        queueItem.Status = SyncQueueStatus.Pending;
                        result.Failed++;
                        result.Messages.Add($"Temporary API error: {queueItem.ErrorMessage}");
                    }
                    else
                    {
                        queueItem.Status = SyncQueueStatus.Conflict;
                        result.Failed++;
                        result.Messages.Add($"Sync conflict: {queueItem.ErrorMessage}");
                    }

                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    queueItem.Status = SyncQueueStatus.Failed;
                    queueItem.ErrorMessage = ex.Message;

                    result.Failed++;
                    result.Messages.Add($"Supplier sync failed: {ex.Message}");

                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }


        private async Task SyncSupplierAsync(
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            queueItem.Status = SyncQueueStatus.Processing;
            queueItem.Attempts++;
            queueItem.ErrorMessage = null;

            await _db.SaveChangesAsync(cancellationToken);

            var supplier = await _db.Suppliers
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == CurrentTenantId &&
                        x.Id == queueItem.LocalEntityId,
                    cancellationToken);

            if (supplier == null)
            {
                queueItem.Status = SyncQueueStatus.Failed;
                queueItem.ErrorMessage = "Local supplier not found.";

                result.Failed++;
                result.Messages.Add($"Local supplier not found for queue item {queueItem.Id}.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Create ||
                queueItem.Operation == "Create")
            {
                await SyncSupplierCreateAsync(supplier, queueItem, result, cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Update ||
                queueItem.Operation == "Update")
            {
                await SyncSupplierUpdateAsync(supplier, queueItem, result, cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Delete ||
                queueItem.Operation == "Delete")
            {
                await SyncSupplierDeleteAsync(supplier, queueItem, result, cancellationToken);
                return;
            }

            queueItem.Status = SyncQueueStatus.Failed;
            queueItem.ErrorMessage = $"Unsupported supplier operation: {queueItem.Operation}";

            result.Failed++;
            result.Messages.Add(queueItem.ErrorMessage);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncSupplierCreateAsync(
            LocalSupplier supplier,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (supplier.ServerId.HasValue && supplier.ServerId.Value != Guid.Empty)
            {
                supplier.SyncStatus = SyncQueueStatus.Done;

                queueItem.Status = SyncQueueStatus.Done;
                queueItem.ProcessedAtUtc = DateTime.UtcNow;
                queueItem.ErrorMessage = null;

                result.Skipped++;
                result.Messages.Add($"Supplier {supplier.Name} already has ServerId.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var request = new CreateSupplierRequest
            {
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Address = supplier.Address,
                City = supplier.City,
                PostalCode = supplier.PostalCode,
                Country = supplier.Country,
                TaxNumber = supplier.TaxNumber,
                PaymentTermsDays = supplier.PaymentTermsDays,
                BankAccount = supplier.BankAccount,
                IsActive = supplier.IsActive,
                Notes = supplier.Notes
            };

            var serverSupplier = await _supplierApi.Create(request);

            supplier.ServerId = serverSupplier.Id;
            supplier.SyncStatus = SyncQueueStatus.Done;
            supplier.LastSyncedAtUtc = DateTime.UtcNow;

            await UpdateLocalPurchasesSupplierServerIdAsync(
                supplier.Id,
                serverSupplier.Id,
                cancellationToken);

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Supplier {supplier.Name} created online.");

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncSupplierUpdateAsync(
            LocalSupplier supplier,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (supplier.ServerId == null || supplier.ServerId == Guid.Empty)
            {
                queueItem.Status = SyncQueueStatus.Pending;
                queueItem.ErrorMessage = "Supplier has no ServerId yet.";

                result.Skipped++;
                result.Messages.Add($"Supplier {supplier.Name} skipped: no ServerId.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var request = new UpdateSupplierRequest
            {
                Id = supplier.ServerId.Value,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Address = supplier.Address,
                City = supplier.City,
                PostalCode = supplier.PostalCode,
                Country = supplier.Country,
                TaxNumber = supplier.TaxNumber,
                PaymentTermsDays = supplier.PaymentTermsDays,
                BankAccount = supplier.BankAccount,
                IsActive = supplier.IsActive,
                Notes = supplier.Notes
            };

            await _supplierApi.Update(supplier.ServerId.Value, request);

            supplier.SyncStatus = SyncQueueStatus.Done;
            supplier.LastSyncedAtUtc = DateTime.UtcNow;

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Supplier {supplier.Name} updated online.");

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncSupplierDeleteAsync(
            LocalSupplier supplier,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (supplier.ServerId.HasValue && supplier.ServerId.Value != Guid.Empty)
            {
                await _supplierApi.Delete(supplier.ServerId.Value);
            }

            supplier.SyncStatus = SyncQueueStatus.Done;
            supplier.LastSyncedAtUtc = DateTime.UtcNow;

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Supplier {supplier.Name} deleted online.");

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateLocalPurchasesSupplierServerIdAsync(
             Guid localSupplierId,
             Guid serverSupplierId,
             CancellationToken cancellationToken)
        {
            var purchases =
                await _db.Purchases
                    .Where(purchase =>
                        purchase.TenantId ==
                            CurrentTenantId &&
                        purchase.SupplierLocalId ==
                            localSupplierId &&
                        (!purchase.SupplierServerId.HasValue ||
                         purchase.SupplierServerId.Value ==
                            Guid.Empty))
                    .ToListAsync(cancellationToken);

            foreach (var purchase in purchases)
            {
                purchase.SupplierServerId =
                    serverSupplierId;
            }
        }
        #endregion

        // ============================================================
        // SALES
        // ============================================================

        #region Sales
        private async Task SyncSalesAsync(
     LocalSyncUploadResult result,
     CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                result);

            var tenantId =
                CurrentTenantId;

            /*
             * Recover sales that were left in Processing because the app,
             * debugger or synchronization operation stopped unexpectedly.
             *
             * This is also done by SyncPendingAsync, but keeping the recovery
             * here makes SyncSalesAsync safe if it is called independently.
             */
            var interruptedSales =
                await _db.SyncQueueItems
                    .Where(queueItem =>
                        queueItem.TenantId == tenantId &&
                        queueItem.EntityName ==
                            SyncEntityName.Sale &&
                        queueItem.Status ==
                            SyncQueueStatus.Processing)
                    .ToListAsync(
                        cancellationToken);

            foreach (var interruptedItem in interruptedSales)
            {
                interruptedItem.Status =
                    SyncQueueStatus.Pending;

                interruptedItem.ErrorMessage =
                    "Recovered after an interrupted sale synchronization.";

                interruptedItem.ProcessedAtUtc =
                    null;
            }

            if (interruptedSales.Count > 0)
            {
                await _db.SaveChangesAsync(
                    cancellationToken);

                _logger.LogWarning(
                    "Recovered {Count} interrupted sale queue items.",
                    interruptedSales.Count);
            }

            /*
             * Retrieve IDs first. Each queue item will then be loaded and
             * processed independently.
             */
            var pendingSaleQueueIds =
                await _db.SyncQueueItems
                    .AsNoTracking()
                    .Where(queueItem =>
                        queueItem.TenantId == tenantId &&
                        queueItem.Status ==
                            SyncQueueStatus.Pending &&
                        queueItem.EntityName ==
                            SyncEntityName.Sale)
                    .OrderBy(queueItem =>
                        queueItem.CreatedAtUtc)
                    .Take(20)
                    .Select(queueItem =>
                        queueItem.Id)
                    .ToListAsync(
                        cancellationToken);

            result.TotalPending +=
                pendingSaleQueueIds.Count;

            _logger.LogInformation(
                "Found {Count} pending sale queue items for tenant {TenantId}.",
                pendingSaleQueueIds.Count,
                tenantId);

            foreach (var queueItemId in pendingSaleQueueIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                /*
                 * Load a fresh queue-item instance for every iteration.
                 */
                var item =
                    await _db.SyncQueueItems
                        .FirstOrDefaultAsync(
                            queueItem =>
                                queueItem.TenantId == tenantId &&
                                queueItem.Id == queueItemId,
                            cancellationToken);

                if (item == null)
                {
                    result.Skipped++;

                    result.Messages.Add(
                        $"Sale queue item {queueItemId} no longer exists.");

                    continue;
                }

                /*
                 * It may have been processed between the ID query and this
                 * query.
                 */
                if (item.Status !=
                    SyncQueueStatus.Pending)
                {
                    result.Skipped++;

                    result.Messages.Add(
                        $"Sale queue item {item.Id} skipped because its " +
                        $"status is {item.Status}.");

                    continue;
                }

                try
                {
                    _logger.LogInformation(
                        "Starting synchronization of sale queue item {QueueId}. " +
                        "LocalSaleId={LocalSaleId}, Attempt={Attempt}.",
                        item.Id,
                        item.LocalEntityId,
                        item.Attempts + 1);

                    await SyncSaleAsync(
                        item,
                        result,
                        cancellationToken);

                    _logger.LogInformation(
                        "Finished synchronization of sale queue item {QueueId}. " +
                        "FinalStatus={Status}.",
                        item.Id,
                        item.Status);
                }
                catch (HttpRequestException exception)
                {
                    item.Status =
                        SyncQueueStatus.Pending;

                    item.ErrorMessage =
                        "API offline. Will retry later.";

                    result.Failed++;

                    result.Messages.Add(
                        $"API offline while synchronizing sale queue item " +
                        $"{item.Id}.");

                    _logger.LogWarning(
                        exception,
                        "API unavailable while synchronizing sale queue item " +
                        "{QueueId}.",
                        item.Id);

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    /*
                     * Stop the rest of the synchronization because the API is
                     * unavailable.
                     */
                    throw;
                }
                catch (ApiException exception)
                {
                    var message =
                        exception.Content ??
                        exception.Message;

                    item.ErrorMessage =
                        message;

                    if (IsAuthError(
                            exception))
                    {
                        item.Status =
                            SyncQueueStatus.Pending;

                        result.Skipped++;

                        result.Messages.Add(
                            $"Sale {item.LocalEntityId} requires a new " +
                            "online login.");
                    }
                    else if (IsTemporaryApiError(
                                 exception))
                    {
                        item.Status =
                            SyncQueueStatus.Pending;

                        result.Failed++;

                        result.Messages.Add(
                            $"Temporary API error while synchronizing sale " +
                            $"{item.LocalEntityId}: {message}");
                    }
                    else
                    {
                        item.Status =
                            SyncQueueStatus.Conflict;

                        result.Failed++;

                        result.Messages.Add(
                            $"Conflict while synchronizing sale " +
                            $"{item.LocalEntityId}: {message}");
                    }

                    _logger.LogError(
                        exception,
                        "Sale API error. QueueId={QueueId}, LocalSaleId={SaleId}, " +
                        "StatusCode={StatusCode}, QueueStatus={QueueStatus}.",
                        item.Id,
                        item.LocalEntityId,
                        exception.StatusCode,
                        item.Status);

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    /*
                     * Leave the item retryable when cancellation happens after
                     * it entered Processing.
                     */
                    item.Status =
                        SyncQueueStatus.Pending;

                    item.ErrorMessage =
                        "Sale synchronization was cancelled and will be retried.";

                    await _db.SaveChangesAsync(
                        CancellationToken.None);

                    throw;
                }
                catch (Exception exception)
                {
                    item.ErrorMessage =
                        exception.Message;

                    if (exception.Message.Contains(
                            "cash session is not synced",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        item.Status =
                            SyncQueueStatus.Pending;

                        result.Skipped++;

                        result.Messages.Add(
                            exception.Message);
                    }
                    else
                    {
                        item.Status =
                            SyncQueueStatus.Failed;

                        result.Failed++;

                        result.Messages.Add(
                            $"Failed synchronizing sale queue item " +
                            $"{item.Id}: {exception.Message}");
                    }

                    _logger.LogError(
                        exception,
                        "Unexpected sale synchronization error. " +
                        "QueueId={QueueId}, LocalSaleId={SaleId}.",
                        item.Id,
                        item.LocalEntityId);

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
            }
        }

        private Task SyncSaleAsync(
    SyncQueueItem queueItem,
    LocalSyncUploadResult uploadResult,
    CancellationToken cancellationToken)
        {
            /*
             * SyncPendingAsync owns LocalDatabaseWriteGate for the complete
             * synchronization operation.
             *
             * Never acquire the same semaphore again here because SemaphoreSlim
             * is not reentrant and would create a deadlock.
             */
            return SyncSaleCoreAsync(
                queueItem,
                uploadResult,
                cancellationToken);
        }

        private async Task SyncSaleCoreAsync(
     SyncQueueItem queueItem,
     LocalSyncUploadResult uploadResult,
     CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                queueItem);

            ArgumentNullException.ThrowIfNull(
                uploadResult);

            var tenantId =
                CurrentTenantId;

            queueItem.Status =
                SyncQueueStatus.Processing;

            queueItem.Attempts++;

            queueItem.LastAttemptAtUtc =
                DateTime.UtcNow;

            queueItem.ErrorMessage =
                null;

            await _db.SaveChangesAsync(
                cancellationToken);

            var sale =
                await _db.Sales
                    .Include(item =>
                        item.Lines)
                    .Include(item =>
                        item.Payments)
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id ==
                                queueItem.LocalEntityId,
                        cancellationToken);

            if (sale == null)
            {
                queueItem.Status =
                    SyncQueueStatus.Failed;

                queueItem.ErrorMessage =
                    "Local sale not found.";

                uploadResult.Failed++;

                uploadResult.Messages.Add(
                    $"Local sale not found for queue item " +
                    $"{queueItem.Id}.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            /*
             * Une vente suspendue ne doit jamais être envoyée
             * comme une vente terminée.
             */
            if (!string.Equals(
                    sale.Status,
                    LocalSaleStatus.Completed,
                    StringComparison.OrdinalIgnoreCase))
            {
                queueItem.Status =
                    SyncQueueStatus.Conflict;

                queueItem.ErrorMessage =
                    $"Sale {sale.LocalInvoiceNumber} is not completed. " +
                    $"Current status: {sale.Status}.";

                sale.SyncStatus =
                    SyncQueueStatus.Conflict;

                uploadResult.Skipped++;

                uploadResult.Messages.Add(
                    $"Sale {sale.LocalInvoiceNumber} skipped because its " +
                    $"status is {sale.Status}.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            /*
             * Une vente est synchronisée uniquement lorsque :
             *
             * - SyncStatus = Done
             * - ServerId est renseigné
             */
            if (sale.SyncStatus ==
                    SyncQueueStatus.Done &&
                sale.ServerId.HasValue &&
                sale.ServerId.Value != Guid.Empty)
            {
                queueItem.Status =
                    SyncQueueStatus.Done;

                queueItem.ServerEntityId =
                    sale.ServerId.Value;

                queueItem.ProcessedAtUtc =
                    sale.LastSyncedAtUtc ??
                    DateTime.UtcNow;

                queueItem.ErrorMessage =
                    null;

                uploadResult.Skipped++;

                uploadResult.Messages.Add(
                    $"Sale {sale.LocalInvoiceNumber} already synchronized " +
                    $"with server id {sale.ServerId.Value}.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            /*
             * Réparation des anciennes données :
             *
             * SyncStatus = Done
             * ServerId absent
             */
            if (sale.SyncStatus ==
                    SyncQueueStatus.Done &&
                (!sale.ServerId.HasValue ||
                 sale.ServerId.Value == Guid.Empty))
            {
                sale.SyncStatus =
                    SyncQueueStatus.Pending;

                queueItem.Status =
                    SyncQueueStatus.Processing;

                queueItem.ErrorMessage =
                    null;

                uploadResult.Messages.Add(
                    $"Sale {sale.LocalInvoiceNumber} was marked Done without " +
                    "a ServerId and will be synchronized again.");

                await _db.SaveChangesAsync(
                    cancellationToken);
            }

            /*
             * La session de caisse doit déjà exister
             * sur le serveur.
             */
            if (!sale.CashSessionServerId.HasValue ||
                sale.CashSessionServerId.Value ==
                    Guid.Empty)
            {
                queueItem.Status =
                    SyncQueueStatus.Pending;

                queueItem.ErrorMessage =
                    "Cash session is not synchronized yet.";

                sale.SyncStatus =
                    SyncQueueStatus.Pending;

                uploadResult.Skipped++;

                uploadResult.Messages.Add(
                    $"Sale {sale.LocalInvoiceNumber} skipped because its " +
                    "cash session is not synchronized yet.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            /*
             * ClientOperationId garantit l’idempotence.
             */
            if (sale.ClientOperationId ==
                Guid.Empty)
            {
                queueItem.Status =
                    SyncQueueStatus.Conflict;

                queueItem.ErrorMessage =
                    "The local sale has no ClientOperationId.";

                sale.SyncStatus =
                    SyncQueueStatus.Conflict;

                uploadResult.Failed++;

                uploadResult.Messages.Add(
                    $"Sale {sale.LocalInvoiceNumber} cannot be synchronized " +
                    "because ClientOperationId is missing.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            /*
             * Le ClientOperationId de la vente
             * est la valeur officielle.
             */
            if (queueItem.ClientOperationId !=
                sale.ClientOperationId)
            {
                queueItem.ClientOperationId =
                    sale.ClientOperationId;

                await _db.SaveChangesAsync(
                    cancellationToken);
            }

            if (sale.Lines.Count == 0)
            {
                queueItem.Status =
                    SyncQueueStatus.Conflict;

                queueItem.ErrorMessage =
                    "The local sale contains no lines.";

                sale.SyncStatus =
                    SyncQueueStatus.Conflict;

                uploadResult.Failed++;

                uploadResult.Messages.Add(
                    $"Sale {sale.LocalInvoiceNumber} cannot be synchronized " +
                    "because it contains no lines.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            var invalidLine =
                sale.Lines.FirstOrDefault(line =>
                    line.ProductLocalId == Guid.Empty ||
                    line.Quantity <= 0m ||
                    line.UnitPrice < 0m ||
                    line.VatRate < 0m ||
                    line.VatRate > 100m);

            if (invalidLine != null)
            {
                queueItem.Status =
                    SyncQueueStatus.Conflict;

                queueItem.ErrorMessage =
                    $"Sale line '{invalidLine.ProductName}' contains " +
                    "invalid synchronization data.";

                sale.SyncStatus =
                    SyncQueueStatus.Conflict;

                uploadResult.Failed++;

                uploadResult.Messages.Add(
                    queueItem.ErrorMessage);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            var request =
                LocalSaleSyncMapper
                    .ToCreateCompleteSaleRequest(
                        sale);

            /*
             * Le serveur doit utiliser ClientOperationId
             * pour rendre cet endpoint idempotent.
             */
            var serverResult =
                await _saleApi.CreateComplete(
                    request);

            if (serverResult == null)
            {
                throw new InvalidOperationException(
                    "The sale API returned an empty response.");
            }

            var serverSale =
                serverResult.Sale
                ?? throw new InvalidOperationException(
                    "The sale API response does not contain " +
                    "the created sale.");

            if (serverSale.Id == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Sale '{sale.LocalInvoiceNumber}' was accepted by the API, " +
                    "but serverResult.Sale.Id is empty.");
            }

            var serverSaleId =
                serverSale.Id;

            var synchronizedAtUtc =
                DateTime.UtcNow;

            /*
             * Mise à jour de la vente locale.
             */
            sale.ServerId =
                serverSaleId;

            if (!string.IsNullOrWhiteSpace(
                    serverSale.InvoiceNumber))
            {
                sale.ServerInvoiceNumber =
                    serverSale.InvoiceNumber.Trim();
            }

            sale.SyncStatus =
                SyncQueueStatus.Done;

            sale.LastSyncedAtUtc =
                synchronizedAtUtc;

            /*
             * Mise à jour de l’élément de la queue.
             */
            queueItem.ServerEntityId =
                serverSaleId;

            queueItem.Status =
                SyncQueueStatus.Done;

            queueItem.ProcessedAtUtc =
                synchronizedAtUtc;

            queueItem.ErrorMessage =
                null;

            /*
             * Mise à jour des paiements locaux.
             */
            foreach (var payment in sale.Payments)
            {
                payment.ServerSaleId =
                    serverSaleId;

                payment.SyncStatus =
                    SyncQueueStatus.Done;

                payment.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }

            /*
             * Mise à jour des mouvements de stock associés.
             */
            var stockMovements =
                await _db.StockMovements
                    .Where(movement =>
                        movement.TenantId == tenantId &&
                        movement.LocalReferenceId ==
                            sale.Id)
                    .ToListAsync(
                        cancellationToken);

            foreach (var movement in stockMovements)
            {
                movement.ServerReferenceId =
                    serverSaleId;

                movement.SyncStatus =
                    SyncQueueStatus.Done;

                movement.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }

            /*
             * Mise à jour des mouvements de caisse associés.
             */
            var cashMovements =
                await _db.CashMovements
                    .Where(movement =>
                        movement.TenantId == tenantId &&
                        movement.LocalReferenceId ==
                            sale.Id)
                    .ToListAsync(
                        cancellationToken);

            foreach (var movement in cashMovements)
            {
                movement.ServerReferenceId =
                    serverSaleId;

                movement.SyncStatus =
                    SyncQueueStatus.Done;

                movement.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }

            /*
             * La transaction de crédit locale a été créée
             * avec la vente.
             *
             * Le serveur crée sa propre transaction financière
             * pendant CreateComplete.
             *
             * La transaction locale ne doit donc pas être
             * uploadée séparément.
             */
            var customerCreditTransactions =
                await _db.CustomerTransactions
                    .Where(transaction =>
                        transaction.TenantId == tenantId &&
                        transaction.SaleLocalId ==
                            sale.Id &&
                        transaction.Origin ==
                            LocalCustomerTransactionOrigin.Sale &&
                        !transaction.UploadRequired)
                    .ToListAsync(
                        cancellationToken);

            foreach (var customerTransaction
                     in customerCreditTransactions)
            {
                customerTransaction.SaleServerId =
                    serverSaleId;

                customerTransaction.SyncStatus =
                    SyncQueueStatus.Done;
            }

            /*
             * La vente, les paiements, les mouvements et
             * les transactions client sont enregistrés ensemble.
             */
            await _db.SaveChangesAsync(
                cancellationToken);

            uploadResult.Synced++;

            uploadResult.Messages.Add(
                $"Sale {sale.LocalInvoiceNumber} synchronized successfully " +
                $"with server id {serverSaleId} and invoice " +
                $"{serverSale.InvoiceNumber}.");
        }

        #endregion


        // ============================================================
        // PURCHASES
        // ============================================================

        #region Purchases

        private async Task SyncPurchasesAsync(
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            var pendingPurchases =
                await _db.SyncQueueItems
                    .Where(queueItem =>
                        queueItem.TenantId == CurrentTenantId &&
                        queueItem.Status == SyncQueueStatus.Pending &&
                        queueItem.EntityName == PurchaseEntityName)
                    .OrderBy(queueItem =>
                        queueItem.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync(cancellationToken);

            result.TotalPending +=
                pendingPurchases.Count;

            foreach (var queueItem in pendingPurchases)
            {
                try
                {
                    await SyncPurchaseAsync(
                        queueItem,
                        result,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (HttpRequestException)
                {
                    queueItem.Status =
                        SyncQueueStatus.Pending;

                    queueItem.ErrorMessage =
                        "API offline. Will retry later.";

                    result.Messages.Add(
                        "API offline while synchronizing purchases.");

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    /*
                     * Stop the complete synchronization because the API
                     * is unavailable.
                     */
                    throw;
                }
                catch (ApiException exception)
                {
                    queueItem.ErrorMessage =
                        exception.Content ??
                        exception.Message;

                    if (IsAuthError(exception))
                    {
                        queueItem.Status =
                            SyncQueueStatus.Pending;

                        result.Skipped++;

                        result.Messages.Add(
                            "Purchase synchronization skipped: " +
                            "log in online again.");
                    }
                    else if (IsTemporaryApiError(exception))
                    {
                        queueItem.Status =
                            SyncQueueStatus.Pending;

                        result.Failed++;

                        result.Messages.Add(
                            "Temporary Purchase API error: " +
                            queueItem.ErrorMessage);
                    }
                    else
                    {
                        queueItem.Status =
                            SyncQueueStatus.Conflict;

                        result.Failed++;

                        result.Messages.Add(
                            "Purchase synchronization conflict: " +
                            queueItem.ErrorMessage);
                    }

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    queueItem.Status =
                        SyncQueueStatus.Failed;

                    queueItem.ErrorMessage =
                        exception
                            .GetBaseException()
                            .Message;

                    result.Failed++;

                    result.Messages.Add(
                        "Purchase synchronization failed: " +
                        queueItem.ErrorMessage);

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
            }
        }

        private async Task SyncPurchaseAsync(
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            queueItem.Status =
                SyncQueueStatus.Processing;

            queueItem.Attempts++;

            queueItem.ErrorMessage =
                null;

            await _db.SaveChangesAsync(
                cancellationToken);

            var purchase =
                await _db.Purchases
                    .Include(item =>
                        item.Lines)
                    .Include(item =>
                        item.Payments)
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == CurrentTenantId &&
                            item.Id == queueItem.LocalEntityId,
                        cancellationToken);

            if (purchase == null)
            {
                queueItem.Status =
                    SyncQueueStatus.Failed;

                queueItem.ErrorMessage =
                    "Local purchase was not found.";

                result.Failed++;

                result.Messages.Add(
                    $"Local purchase was not found for queue item " +
                    $"{queueItem.Id}.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            if (!IsCreateOperation(queueItem))
            {
                queueItem.Status =
                    SyncQueueStatus.Failed;

                queueItem.ErrorMessage =
                    $"Unsupported purchase operation: " +
                    $"{queueItem.Operation}";

                result.Failed++;

                result.Messages.Add(
                    queueItem.ErrorMessage);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            /*
             * The operation id must never change between retries.
             */
            if (purchase.ClientOperationId == Guid.Empty)
            {
                queueItem.Status =
                    SyncQueueStatus.Conflict;

                queueItem.ErrorMessage =
                    "The local purchase has no ClientOperationId.";

                purchase.SyncStatus =
                    SyncQueueStatus.Conflict;

                result.Failed++;

                result.Messages.Add(
                    $"Purchase {purchase.LocalPurchaseNumber} cannot be " +
                    "synchronized because ClientOperationId is missing.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            /*
             * The purchase ClientOperationId is authoritative.
             */
            if (queueItem.ClientOperationId !=
                purchase.ClientOperationId)
            {
                queueItem.ClientOperationId =
                    purchase.ClientOperationId;
            }

            /*
             * A ServerId means the purchase was already uploaded or
             * downloaded during a pull synchronization.
             */
            if (purchase.ServerId.HasValue &&
                purchase.ServerId.Value != Guid.Empty)
            {
                await FinalizePurchaseSynchronizationAsync(
                    purchase,
                    queueItem,
                    purchase.ServerId.Value,
                    cancellationToken);

                result.Skipped++;

                result.Messages.Add(
                    $"Purchase {purchase.LocalPurchaseNumber} " +
                    "already has a ServerId.");

                return;
            }

            if (purchase.Lines.Count == 0)
            {
                queueItem.Status =
                    SyncQueueStatus.Conflict;

                queueItem.ErrorMessage =
                    "The local purchase contains no lines.";

                purchase.SyncStatus =
                    SyncQueueStatus.Conflict;

                result.Failed++;

                result.Messages.Add(
                    $"Purchase {purchase.LocalPurchaseNumber} " +
                    "contains no lines.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            var dependenciesReady =
                await ResolvePurchaseDependenciesAsync(
                    purchase,
                    cancellationToken);

            if (!dependenciesReady)
            {
                queueItem.Status =
                    SyncQueueStatus.Pending;

                queueItem.ErrorMessage =
                    "The supplier or one of the purchase products " +
                    "has not been synchronized yet.";

                purchase.SyncStatus =
                    SyncQueueStatus.Pending;

                result.Skipped++;

                result.Messages.Add(
                    $"Purchase {purchase.LocalPurchaseNumber} skipped: " +
                    "missing supplier or product ServerId.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            if (!purchase.SupplierServerId.HasValue ||
                purchase.SupplierServerId.Value == Guid.Empty)
            {
                queueItem.Status =
                    SyncQueueStatus.Pending;

                queueItem.ErrorMessage =
                    "The supplier has no ServerId.";

                purchase.SyncStatus =
                    SyncQueueStatus.Pending;

                result.Skipped++;

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            var invalidLine =
                purchase.Lines
                    .FirstOrDefault(line =>
                        !line.ProductServerId.HasValue ||
                        line.ProductServerId.Value == Guid.Empty ||
                        line.QuantityReceived <= 0m ||
                        line.UnitPurchasePrice < 0m ||
                        line.VatRate < 0m ||
                        line.VatRate > 100m);

            if (invalidLine != null)
            {
                queueItem.Status =
                    SyncQueueStatus.Conflict;

                queueItem.ErrorMessage =
                    $"Purchase line '{invalidLine.ProductName}' " +
                    "contains invalid synchronization data.";

                purchase.SyncStatus =
                    SyncQueueStatus.Conflict;

                result.Failed++;

                result.Messages.Add(
                    queueItem.ErrorMessage);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            var request =
                new CreateCompletePurchaseRequest
                {
                    ClientOperationId =
                        purchase.ClientOperationId,

                    SupplierId =
                        purchase.SupplierServerId.Value,

                    PurchaseDate =
                        purchase.PurchaseDateUtc,

                    Lines =
                        purchase.Lines
                            .Select(line =>
                                new PurchaseLineItem
                                {
                                    /*
                                     * For a pack this remains the ServerId of
                                     * the pack product.
                                     *
                                     * The server PurchaseService converts the
                                     * pack quantity into unit stock.
                                     */
                                    ProductId =
                                        line.ProductServerId!.Value,

                                    /*
                                     * For a pack this remains the number
                                     * of packs purchased.
                                     */
                                    Quantity =
                                        line.QuantityReceived,

                                    /*
                                     * LocalPurchaseLine already contains the
                                     * effective unit purchase price.
                                     */
                                    UnitPrice =
                                        line.UnitPurchasePrice,

                                    VatRate =
                                        line.VatRate,

                                    /*
                                     * Do not apply the discount twice.
                                     */
                                    DiscountPercent =
                                        0m
                                })
                            .ToList(),

                    /*
                     * LocalPurchaseService currently creates no payment.
                     * Payment synchronization can be added separately.
                     */
                    Payment =
                        null
                };

            /*
             * The server endpoint must be idempotent through
             * ClientOperationId.
             */
            var serverPurchase =
                await _purchaseApi.CreateComplete(
                    request,
                    cancellationToken);

            await FinalizePurchaseSynchronizationAsync(
                purchase,
                queueItem,
                serverPurchase.Id,
                cancellationToken);

            result.Synced++;

            result.Messages.Add(
                $"Purchase {purchase.LocalPurchaseNumber} " +
                "synchronized successfully.");
        }

        private async Task<bool> ResolvePurchaseDependenciesAsync(
            LocalPurchase purchase,
            CancellationToken cancellationToken)
        {
            var allDependenciesReady =
                true;

            /*
             * Resolve the supplier ServerId.
             */
            if (!purchase.SupplierServerId.HasValue ||
    purchase.SupplierServerId.Value == Guid.Empty)
            {
                if (purchase.SupplierLocalId == Guid.Empty)
                {
                    allDependenciesReady =
                        false;
                }
                else
                {
                    var supplier =
                        await _db.Suppliers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                item =>
                                    item.TenantId ==
                                        CurrentTenantId &&
                                    item.Id ==
                                        purchase.SupplierLocalId &&
                                    !item.IsDeleted,
                                cancellationToken);

                    if (supplier?.ServerId.HasValue == true &&
                        supplier.ServerId.Value != Guid.Empty)
                    {
                        purchase.SupplierServerId =
                            supplier.ServerId.Value;
                    }
                    else
                    {
                        allDependenciesReady =
                            false;
                    }
                }
            }

            /*
             * ProductLocalId is always the selected purchase product.
             *
             * For a pack, ProductLocalId is the pack local id—not the
             * linked unit-product id. Therefore ProductServerId becomes
             * the ServerId of the pack, which is exactly what the API needs.
             */
            var unresolvedProductLocalIds =
                purchase.Lines
                    .Where(line =>
                        !line.ProductServerId.HasValue ||
                        line.ProductServerId.Value == Guid.Empty)
                    .Select(line =>
                        line.ProductLocalId)
                    .Distinct()
                    .ToList();

            if (unresolvedProductLocalIds.Count == 0)
            {
                return allDependenciesReady;
            }

            var localProducts =
                await _db.Products
                    .AsNoTracking()
                    .Where(product =>
                        product.TenantId == CurrentTenantId &&
                        unresolvedProductLocalIds.Contains(
                            product.Id) &&
                        !product.IsDeletedLocally)
                    .ToListAsync(cancellationToken);

            var productMap =
                localProducts
                    .ToDictionary(product =>
                        product.Id);

            foreach (var line in purchase.Lines)
            {
                if (line.ProductServerId.HasValue &&
                    line.ProductServerId.Value != Guid.Empty)
                {
                    continue;
                }

                if (productMap.TryGetValue(
                        line.ProductLocalId,
                        out var product) &&
                    product.ServerId.HasValue &&
                    product.ServerId.Value != Guid.Empty)
                {
                    line.ProductServerId =
                        product.ServerId.Value;
                }
                else
                {
                    allDependenciesReady =
                        false;
                }
            }

            return allDependenciesReady;
        }

        private async Task FinalizePurchaseSynchronizationAsync(
            LocalPurchase purchase,
            SyncQueueItem queueItem,
            Guid serverPurchaseId,
            CancellationToken cancellationToken)
        {
            var synchronizedAtUtc =
                DateTime.UtcNow;

            purchase.ServerId =
                serverPurchaseId;

            purchase.SyncStatus =
                SyncQueueStatus.Done;

            purchase.LastSyncedAtUtc =
                synchronizedAtUtc;

            queueItem.Status =
                SyncQueueStatus.Done;

            queueItem.ProcessedAtUtc =
                synchronizedAtUtc;

            queueItem.ErrorMessage =
                null;

            /*
             * Local stock was already increased when the purchase was
             * created in SQLite. Never increase it here again.
             */
            var stockMovements =
                await _db.StockMovements
                    .Where(movement =>
                        movement.TenantId == CurrentTenantId &&
                        movement.LocalReferenceId == purchase.Id)
                    .ToListAsync(cancellationToken);

            foreach (var movement in stockMovements)
            {
                movement.ServerReferenceId =
                    serverPurchaseId;

                movement.SyncStatus =
                    SyncQueueStatus.Done;

                movement.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }

            /*
             * Payments are not marked Done because Payment=null was sent.
             * Only associate them with the server purchase.
             */
            foreach (var payment in purchase.Payments)
            {
                payment.ServerPurchaseId =
                    serverPurchaseId;
            }

            await _db.SaveChangesAsync(
                cancellationToken);
        }

        private static bool IsCreateOperation(
            SyncQueueItem queueItem)
        {
            return queueItem.Operation ==
                       SyncOperation.Create ||
                   string.Equals(
                       queueItem.Operation,
                       "Create",
                       StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        // ============================================================
        // PRODUCTS
        // ============================================================

        #region Products

        private async Task SyncProductsAsync(
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            var pendingProducts = await _db.SyncQueueItems
                .Where(x =>
                    x.TenantId == CurrentTenantId &&
                    x.Status == SyncQueueStatus.Pending &&
                    x.EntityName == ProductEntityName)
                .OrderBy(x => x.CreatedAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken);

            result.TotalPending += pendingProducts.Count;

            foreach (var queueItem in pendingProducts)
            {
                try
                {
                    await SyncProductAsync(queueItem, result, cancellationToken);
                }
                catch (HttpRequestException)
                {
                    queueItem.Status = SyncQueueStatus.Pending;
                    queueItem.ErrorMessage = "API offline. Will retry later.";

                    result.Messages.Add("API offline while syncing products.");

                    await _db.SaveChangesAsync(cancellationToken);
                    throw;
                }
                catch (ApiException ex)
                {
                    queueItem.ErrorMessage = ex.Content ?? ex.Message;

                    if (IsAuthError(ex))
                    {
                        queueItem.Status = SyncQueueStatus.Pending;

                        result.Skipped++;
                        result.Messages.Add($"{queueItem.EntityName} sync skipped: login online again.");
                    }
                    else if (IsTemporaryApiError(ex))
                    {
                        queueItem.Status = SyncQueueStatus.Pending;

                        result.Failed++;
                        result.Messages.Add($"Temporary product sync API error: {queueItem.ErrorMessage}");
                    }
                    else
                    {
                        queueItem.Status = SyncQueueStatus.Conflict;

                        result.Failed++;
                        result.Messages.Add($"Product sync conflict: {queueItem.ErrorMessage}");
                    }

                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    queueItem.Status = SyncQueueStatus.Failed;
                    queueItem.ErrorMessage = ex.Message;

                    result.Failed++;
                    result.Messages.Add($"Product sync failed: {ex.Message}");

                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private async Task SyncProductAsync(
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            queueItem.Status = SyncQueueStatus.Processing;
            queueItem.Attempts++;
            queueItem.ErrorMessage = null;

            await _db.SaveChangesAsync(cancellationToken);

            var product = await _db.Products
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == CurrentTenantId &&
                        x.Id == queueItem.LocalEntityId,
                    cancellationToken);

            if (product == null)
            {
                queueItem.Status = SyncQueueStatus.Failed;
                queueItem.ErrorMessage = "Local product not found.";

                result.Failed++;
                result.Messages.Add($"Local product not found for queue item {queueItem.Id}.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Create ||
                queueItem.Operation == "Create")
            {
                await SyncProductCreateAsync(product, queueItem, result, cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Update ||
                queueItem.Operation == "Update")
            {
                await SyncProductUpdateAsync(product, queueItem, result, cancellationToken);
                return;
            }

            if (queueItem.Operation == SyncOperation.Delete ||
                queueItem.Operation == "Delete")
            {
                await SyncProductDeleteAsync(product, queueItem, result, cancellationToken);
                return;
            }

            queueItem.Status = SyncQueueStatus.Failed;
            queueItem.ErrorMessage = $"Unsupported product operation: {queueItem.Operation}";

            result.Failed++;
            result.Messages.Add(queueItem.ErrorMessage);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncProductCreateAsync(
            LocalProduct product,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (product.ServerId.HasValue && product.ServerId.Value != Guid.Empty)
            {
                product.SyncStatus = SyncQueueStatus.Done;

                queueItem.Status = SyncQueueStatus.Done;
                queueItem.ProcessedAtUtc = DateTime.UtcNow;
                queueItem.ErrorMessage = null;

                result.Skipped++;
                result.Messages.Add($"Product {product.Name} already has ServerId.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            if (product.CatalogProductId == null || product.CatalogProductId == Guid.Empty)
            {
                product.SyncStatus = SyncQueueStatus.Conflict;

                queueItem.Status = SyncQueueStatus.Conflict;
                queueItem.ErrorMessage = "Product has no CatalogProductId.";

                result.Failed++;
                result.Messages.Add($"Product {product.Name} conflict: missing CatalogProductId.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var request = new CreateProductRequest
            {
                CatalogProductId = product.CatalogProductId.Value,
                SalePrice = product.SalePrice,
                SalePrice2 = product.SalePrice2,
                SalePrice3 = product.SalePrice3,
                PurchasePrice = product.PurchasePrice,
                VatRate = product.VatRate,
                MinStockLevel = product.MinStockLevel,
                MaxStockLevel = product.MaxStockLevel,
                IsTracked = product.IsTracked,
                IsActive = product.Status
            };

            var serverProduct = await _productApi.Create(request);

            product.ServerId = serverProduct.Id;
            product.SyncStatus = SyncQueueStatus.Done;
            product.LastSyncedAtUtc = DateTime.UtcNow;

            await UpdateDependentDamageProductServerIdsAsync(
                product.Id,
                serverProduct.Id,
                cancellationToken);

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Product {product.Name} created online.");

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncProductUpdateAsync(
            LocalProduct product,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (product.ServerId == null || product.ServerId == Guid.Empty)
            {
                queueItem.Status = SyncQueueStatus.Pending;
                queueItem.ErrorMessage = "Product has no ServerId yet.";

                result.Skipped++;
                result.Messages.Add($"Product {product.Name} skipped: no ServerId.");

                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var request = new UpdateProductRequest
            {
                SalePrice = product.SalePrice,
                SalePrice2 = product.SalePrice2,
                SalePrice3 = product.SalePrice3,
                PurchasePrice = product.PurchasePrice,
                VatRate = product.VatRate,
                MinStockLevel = product.MinStockLevel,
                MaxStockLevel = product.MaxStockLevel,
                IsActive = product.Status,
                IsTracked = product.IsTracked
            };

            await _productApi.Update(product.ServerId.Value, request);

            product.SyncStatus = SyncQueueStatus.Done;
            product.LastSyncedAtUtc = DateTime.UtcNow;

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Product {product.Name} updated online.");

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncProductDeleteAsync(
            LocalProduct product,
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            if (product.ServerId.HasValue && product.ServerId.Value != Guid.Empty)
            {
                await _productApi.Delete(product.ServerId.Value);
            }

            product.SyncStatus = SyncQueueStatus.Done;
            product.LastSyncedAtUtc = DateTime.UtcNow;

            queueItem.Status = SyncQueueStatus.Done;
            queueItem.ProcessedAtUtc = DateTime.UtcNow;
            queueItem.ErrorMessage = null;

            result.Synced++;
            result.Messages.Add($"Product {product.Name} deleted online.");

            await _db.SaveChangesAsync(cancellationToken);
        }


        private async Task UpdateDependentDamageProductServerIdsAsync(
            Guid localProductId,
            Guid serverProductId,
            CancellationToken cancellationToken)
        {
            var damages =
                await _db.Damages
                    .Where(damage =>
                        damage.TenantId == CurrentTenantId &&
                        damage.ProductLocalId == localProductId &&
                        (!damage.ProductServerId.HasValue ||
                         damage.ProductServerId.Value == Guid.Empty))
                    .ToListAsync(cancellationToken);

            foreach (var damage in damages)
            {
                damage.ProductServerId =
                    serverProductId;
            }
        }

        #endregion

        // ============================================================
        // DAMAGES
        // ============================================================

        #region Damages

        private async Task SyncDamagesAsync(
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            var pendingDamages =
                await _db.SyncQueueItems
                    .Where(queueItem =>
                        queueItem.TenantId ==
                            CurrentTenantId &&
                        queueItem.Status ==
                            SyncQueueStatus.Pending &&
                        queueItem.EntityName ==
                            DamageEntityName)
                    .OrderBy(queueItem =>
                        queueItem.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync(cancellationToken);

            result.TotalPending +=
                pendingDamages.Count;

            foreach (var queueItem in pendingDamages)
            {
                try
                {
                    await SyncDamageAsync(
                        queueItem,
                        result,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (HttpRequestException)
                {
                    queueItem.Status =
                        SyncQueueStatus.Pending;

                    queueItem.ErrorMessage =
                        "API offline. Will retry later.";

                    result.Messages.Add(
                        "API offline while synchronizing damages.");

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    throw;
                }
                catch (ApiException exception)
                {
                    queueItem.ErrorMessage =
                        exception.Content ??
                        exception.Message;

                    if (IsAuthError(exception))
                    {
                        queueItem.Status =
                            SyncQueueStatus.Pending;

                        result.Skipped++;

                        result.Messages.Add(
                            "Damage synchronization skipped: " +
                            "log in online again.");
                    }
                    else if (IsTemporaryApiError(exception))
                    {
                        queueItem.Status =
                            SyncQueueStatus.Pending;

                        result.Failed++;

                        result.Messages.Add(
                            $"Temporary Damage API error: " +
                            $"{queueItem.ErrorMessage}");
                    }
                    else
                    {
                        queueItem.Status =
                            SyncQueueStatus.Conflict;

                        result.Failed++;

                        result.Messages.Add(
                            $"Damage synchronization conflict: " +
                            $"{queueItem.ErrorMessage}");
                    }

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    queueItem.Status =
                        SyncQueueStatus.Failed;

                    queueItem.ErrorMessage =
                        exception.GetBaseException().Message;

                    result.Failed++;

                    result.Messages.Add(
                        $"Damage synchronization failed: " +
                        $"{queueItem.ErrorMessage}");

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
            }
        }

        private async Task SyncDamageAsync(
            SyncQueueItem queueItem,
            LocalSyncUploadResult result,
            CancellationToken cancellationToken)
        {
            queueItem.Status =
                SyncQueueStatus.Processing;

            queueItem.Attempts++;

            queueItem.ErrorMessage =
                null;

            if (queueItem.ClientOperationId ==
                Guid.Empty)
            {
                queueItem.ClientOperationId =
                    Guid.NewGuid();
            }

            await _db.SaveChangesAsync(
                cancellationToken);

            var damage =
                await _db.Damages
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId ==
                                CurrentTenantId &&
                            item.Id ==
                                queueItem.LocalEntityId,
                        cancellationToken);

            if (damage == null)
            {
                queueItem.Status =
                    SyncQueueStatus.Failed;

                queueItem.ErrorMessage =
                    "Local damage was not found.";

                result.Failed++;

                result.Messages.Add(
                    $"Local damage was not found for queue item " +
                    $"{queueItem.Id}.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            if (queueItem.Operation !=
                SyncOperation.Create &&
                !string.Equals(
                    queueItem.Operation,
                    "Create",
                    StringComparison.OrdinalIgnoreCase))
            {
                queueItem.Status =
                    SyncQueueStatus.Failed;

                queueItem.ErrorMessage =
                    $"Unsupported damage operation: " +
                    $"{queueItem.Operation}";

                result.Failed++;

                result.Messages.Add(
                    queueItem.ErrorMessage);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            if (damage.ServerId.HasValue &&
                damage.ServerId.Value != Guid.Empty)
            {
                damage.SyncStatus =
                    SyncQueueStatus.Done;

                damage.LocalStatus =
                    LocalDamageStatus.Synced;

                damage.LastSyncedAtUtc =
                    DateTime.UtcNow;

                queueItem.Status =
                    SyncQueueStatus.Done;

                queueItem.ProcessedAtUtc =
                    DateTime.UtcNow;

                queueItem.ErrorMessage =
                    null;

                result.Skipped++;

                result.Messages.Add(
                    $"Damage {damage.DamageNumber} already has a ServerId.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            if (!damage.ProductServerId.HasValue ||
                damage.ProductServerId.Value ==
                    Guid.Empty)
            {
                var localProduct =
                    await _db.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            product =>
                                product.TenantId ==
                                    CurrentTenantId &&
                                product.Id ==
                                    damage.ProductLocalId,
                            cancellationToken);

                if (localProduct?.ServerId.HasValue ==
                        true &&
                    localProduct.ServerId.Value !=
                        Guid.Empty)
                {
                    damage.ProductServerId =
                        localProduct.ServerId.Value;
                }
            }

            if (!damage.ProductServerId.HasValue ||
                damage.ProductServerId.Value ==
                    Guid.Empty)
            {
                queueItem.Status =
                    SyncQueueStatus.Pending;

                queueItem.ErrorMessage =
                    "The product has not been synchronized yet.";

                result.Skipped++;

                result.Messages.Add(
                    $"Damage {damage.DamageNumber} skipped: " +
                    "the product has no ServerId.");

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            var request =
                new CompleteDamageRequest
                {
                    ClientOperationId =
                        queueItem.ClientOperationId,

                    ProductId =
                        damage.ProductServerId.Value,

                    Quantity =
                        damage.Quantity,

                    Reason =
                        damage.Reason,

                    DamageDate =
                        damage.DamageDateUtc
                };

            /*
             * The endpoint must be atomic and idempotent.
             * Retrying the same ClientOperationId must return
             * the existing DamageResult without reducing stock again.
             */
            var serverDamage =
                await _damageApi.Complete(
                    request,
                    cancellationToken);

            /*
             * Reconcile the existing SQLite row.
             * LocalDamageSyncService also completes the queue item.
             * It does not reduce LocalStock a second time.
             */
            await _damageSync.UpsertFromServerAsync(
                serverDamage,
                damage.Id,
                cancellationToken);

            result.Synced++;

            result.Messages.Add(
                $"Damage {damage.DamageNumber} synchronized successfully.");
        }

        #endregion

    }
}