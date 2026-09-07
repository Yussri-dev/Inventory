#if DEBUG
using Inventory.Dto.Suppliers.Requests;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.Ui.Services.Sync.Testing
{
    public sealed class SupplierBulkLoadSeeder
    {
        private const int MaximumCount =
            5_000;

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public SupplierBulkLoadSeeder(
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

            var suppliers =
                new List<LocalSupplier>(
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

                var localSupplierId =
                    Guid.NewGuid();

                var name =
                    $"SUPPLIER-LOAD-{runId}-{index:D5}";

                var request =
                    new CreateSupplierRequest
                    {
                        Name =
                            name,

                        ContactPerson =
                            null,

                        Email =
                            null,

                        Phone =
                            null,

                        Address =
                            null,

                        City =
                            null,

                        PostalCode =
                            null,

                        Country =
                            null,

                        TaxNumber =
                            null,

                        PaymentTermsDays =
                            30,

                        BankAccount =
                            null,

                        IsActive =
                            true,

                        Notes =
                            "Bulk synchronization load test."
                    };

                suppliers.Add(
                    new LocalSupplier
                    {
                        Id =
                            localSupplierId,

                        TenantId =
                            tenantId,

                        ServerId =
                            null,

                        Name =
                            name,

                        ContactPerson =
                            null,

                        Email =
                            null,

                        Phone =
                            null,

                        Address =
                            null,

                        City =
                            null,

                        PostalCode =
                            null,

                        Country =
                            null,

                        TaxNumber =
                            null,

                        PaymentTermsDays =
                            request.PaymentTermsDays,

                        BankAccount =
                            null,

                        CurrentBalance =
                            0m,

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
                            localSupplierId,

                        ServerEntityId =
                            null,

                        EntityName =
                            "Supplier",

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
                _db.Suppliers.AddRange(
                    suppliers);

                _db.SyncQueueItems.AddRange(
                    queueItems);

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