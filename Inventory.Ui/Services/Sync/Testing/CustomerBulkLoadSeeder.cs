#if DEBUG
using Inventory.Dto.Customers.Requests;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.Ui.Services.Sync.Testing
{
    public sealed class CustomerBulkLoadSeeder
    {
        private const int MaximumCount =
            5_000;

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public CustomerBulkLoadSeeder(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            ArgumentNullException.ThrowIfNull(
                db);

            ArgumentNullException.ThrowIfNull(
                tenantContext);

            _db =
                db;

            _tenantContext =
                tenantContext;
        }

        public async Task<int> SeedAsync(
            int count = MaximumCount,
            CancellationToken cancellationToken = default)
        {
            count =
                Math.Clamp(
                    count,
                    1,
                    MaximumCount);

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            var now =
                DateTime.UtcNow;

            var runId =
                Guid.NewGuid()
                    .ToString("N")[..8];

            var customers =
                new List<LocalCustomer>(
                    count);

            var queueItems =
                new List<SyncQueueItem>(
                    count);

            for (var index = 1;
                 index <= count;
                 index++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var createdAtUtc =
                    now.AddTicks(
                        index);

                var localCustomerId =
                    Guid.NewGuid();

                var name =
                    $"LOAD-{runId}-{index:D5}";

                var request =
                    new CreateCustomerRequest
                    {
                        Name =
                            name,

                        Email =
                            null,

                        Phone =
                            null,

                        Address =
                            null,

                        TaxNumber =
                            null,

                        CreditLimit =
                            0m,

                        AllowCredit =
                            false,

                        HasUnlimitedCredit =
                            false,

                        IsActive =
                            true,

                        Notes =
                            "Bulk synchronization load test."
                    };

                customers.Add(
                    new LocalCustomer
                    {
                        Id =
                            localCustomerId,

                        TenantId =
                            tenantId,

                        ServerId =
                            null,

                        Name =
                            name,

                        Email =
                            null,

                        Phone =
                            null,

                        Address =
                            null,

                        TaxNumber =
                            null,

                        CreditLimit =
                            0m,

                        CurrentBalance =
                            0m,

                        AllowCredit =
                            false,

                        HasUnlimitedCredit =
                            false,

                        IsActive =
                            true,

                        IsDeleted =
                            false,

                        Notes =
                            request.Notes,

                        SyncStatus =
                            SyncQueueStatus.Pending,

                        CreatedAtUtc =
                            createdAtUtc
                    });

                queueItems.Add(
                    new SyncQueueItem
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ClientOperationId =
                            Guid.NewGuid(),

                        LocalEntityId =
                            localCustomerId,

                        ServerEntityId =
                            null,

                        EntityName =
                            "Customer",

                        Operation =
                            SyncOperation.Create,

                        PayloadJson =
                            JsonSerializer.Serialize(
                                request),

                        Status =
                            SyncQueueStatus.Pending,

                        Attempts =
                            0,

                        ErrorMessage =
                            null,

                        CreatedAtUtc =
                            createdAtUtc,

                        LastAttemptAtUtc =
                            null,

                        ProcessedAtUtc =
                            null,

                        NextAttemptAtUtc =
                            null,

                        BatchId =
                            null,

                        LockedAtUtc =
                            null
                    });
            }

            await using var transaction =
                await _db.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                _db.Customers.AddRange(
                    customers);

                _db.SyncQueueItems.AddRange(
                    queueItems);

                /*
                 * One SaveChanges call ensures the test measures bulk
                 * processing instead of 5,000 individual transactions.
                 */
                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                return count;
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                _db.ChangeTracker.Clear();

                throw;
            }
        }
    }
}
#endif