using Inventory.Domain.Models;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Services.Sync
{
    public sealed class SyncOperationExecutor
        : ISyncOperationExecutor
    {
        private const string ProcessingStatus =
            "Processing";

        private readonly InventoryDbContext _db;
        private readonly ITenantContext _tenantContext;

        public SyncOperationExecutor(
            InventoryDbContext db,
            ITenantContext tenantContext)
        {
            _db =
                db;

            _tenantContext =
                tenantContext;
        }

        public async Task<SyncBatchItemResult> ExecuteAsync(
            Guid batchId,
            SyncBatchOperationRequest operation,
            ISyncOperationHandler handler,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                operation);

            ArgumentNullException.ThrowIfNull(
                handler);

            if (batchId == Guid.Empty)
            {
                throw new ArgumentException(
                    "BatchId is required.",
                    nameof(batchId));
            }

            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            var payloadHash =
                CalculatePayloadHash(
                    operation);

            var existingRecord =
                await FindExistingAsync(
                    tenantId,
                    operation.ClientOperationId,
                    cancellationToken);

            if (existingRecord != null)
            {
                return CreateExistingResult(
                    operation,
                    existingRecord,
                    payloadHash);
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken);

            try
            {
                /*
                 * Inserted before handler execution so the unique
                 * TenantId + ClientOperationId index locks this operation.
                 */
                var record =
                    new SyncOperationRecord
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        BatchId =
                            batchId,

                        QueueItemId =
                            operation.QueueItemId,

                        ClientOperationId =
                            operation.ClientOperationId,

                        LocalEntityId =
                            operation.LocalEntityId,

                        ServerEntityId =
                            operation.ServerEntityId,

                        ServerReferenceNumber =
                            null,

                        EntityName =
                            operation.EntityName.Trim(),

                        Operation =
                            operation.Operation.Trim(),

                        Status =
                            ProcessingStatus,

                        PayloadHash =
                            payloadHash,

                        ErrorMessage =
                            null,

                        ReceivedAtUtc =
                            DateTime.UtcNow,

                        ProcessedAtUtc =
                            null,

                        CreatedAt =
                            DateTime.UtcNow,

                        CreatedByUserId =
                            userId,

                        IsDeleted =
                            false
                    };

                _db.SyncOperationRecords.Add(
                    record);

                await _db.SaveChangesAsync(
                    cancellationToken);

                var itemResult =
                    await handler.ProcessAsync(
                        operation,
                        cancellationToken);

                if (itemResult == null)
                {
                    await RollbackAndClearAsync(
                        transaction,
                        cancellationToken);

                    return CreateFailedResult(
                        operation,
                        "The synchronization handler returned no result.");
                }

                /*
                 * Temporary failures are rolled back, including the
                 * Processing record and all handler changes.
                 */
                if (IsStatus(
                        itemResult.Status,
                        SyncBatchItemStatus.Failed))
                {
                    await RollbackAndClearAsync(
                        transaction,
                        cancellationToken);

                    return NormalizeResult(
                        operation,
                        itemResult);
                }

                if (!IsTerminalStatus(
                        itemResult.Status))
                {
                    await RollbackAndClearAsync(
                        transaction,
                        cancellationToken);

                    return CreateFailedResult(
                        operation,
                        "The synchronization handler returned an " +
                        "unsupported status.");
                }

                record.ServerEntityId =
                    itemResult.ServerEntityId ??
                    operation.ServerEntityId;

                record.ServerReferenceNumber =
                    Truncate(
                        itemResult.ServerReferenceNumber,
                        200);

                record.Status =
                    itemResult.Status;

                record.ErrorMessage =
                    Truncate(
                        itemResult.ErrorMessage,
                        2_000);

                record.ProcessedAtUtc =
                    DateTime.UtcNow;

                record.ModifiedAt =
                    DateTime.UtcNow;

                record.ModifiedByUserId =
                    userId;

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return NormalizeResult(
                    operation,
                    itemResult);
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(
                    exception))
            {
                await RollbackAndClearAsync(
                    transaction,
                    cancellationToken);

                /*
                 * Another request committed the same operation while
                 * this request was starting.
                 */
                var concurrentRecord =
                    await FindExistingAsync(
                        tenantId,
                        operation.ClientOperationId,
                        cancellationToken);

                if (concurrentRecord == null)
                {
                    throw;
                }

                return CreateExistingResult(
                    operation,
                    concurrentRecord,
                    payloadHash);
            }
            catch
            {
                await RollbackAndClearAsync(
                    transaction,
                    cancellationToken);

                throw;
            }
        }

        private async Task<SyncOperationRecord?>
            FindExistingAsync(
                Guid tenantId,
                Guid clientOperationId,
                CancellationToken cancellationToken)
        {
            return await _db.SyncOperationRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    record =>
                        record.TenantId == tenantId &&
                        record.ClientOperationId ==
                            clientOperationId &&
                        !record.IsDeleted,
                    cancellationToken);
        }

        private static SyncBatchItemResult
            CreateExistingResult(
                SyncBatchOperationRequest operation,
                SyncOperationRecord existingRecord,
                string payloadHash)
        {
            var sameOperation =
                string.Equals(
                    existingRecord.EntityName,
                    operation.EntityName.Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    existingRecord.Operation,
                    operation.Operation.Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                existingRecord.LocalEntityId ==
                    operation.LocalEntityId &&
                string.Equals(
                    existingRecord.PayloadHash,
                    payloadHash,
                    StringComparison.OrdinalIgnoreCase);

            if (!sameOperation)
            {
                return new SyncBatchItemResult
                {
                    QueueItemId =
                        operation.QueueItemId,

                    ClientOperationId =
                        operation.ClientOperationId,

                    ServerEntityId =
                        existingRecord.ServerEntityId,

                    ServerReferenceNumber =
                        existingRecord.ServerReferenceNumber,

                    Status =
                        SyncBatchItemStatus.Conflict,

                    ErrorMessage =
                        "ClientOperationId was already used with " +
                        "different synchronization data."
                };
            }

            if (IsStatus(
                    existingRecord.Status,
                    SyncBatchItemStatus.Conflict))
            {
                return new SyncBatchItemResult
                {
                    QueueItemId =
                        operation.QueueItemId,

                    ClientOperationId =
                        operation.ClientOperationId,

                    ServerEntityId =
                        existingRecord.ServerEntityId,

                    ServerReferenceNumber =
                        existingRecord.ServerReferenceNumber,

                    Status =
                        SyncBatchItemStatus.Conflict,

                    ErrorMessage =
                        existingRecord.ErrorMessage
                };
            }

            if (IsStatus(
                    existingRecord.Status,
                    SyncBatchItemStatus.Done) ||
                IsStatus(
                    existingRecord.Status,
                    SyncBatchItemStatus.Duplicate))
            {
                return new SyncBatchItemResult
                {
                    QueueItemId =
                        operation.QueueItemId,

                    ClientOperationId =
                        operation.ClientOperationId,

                    ServerEntityId =
                        existingRecord.ServerEntityId,

                    ServerReferenceNumber =
                        existingRecord.ServerReferenceNumber,

                    Status =
                        SyncBatchItemStatus.Duplicate,

                    ErrorMessage =
                        null
                };
            }

            return CreateFailedResult(
                operation,
                "The same synchronization operation is already " +
                "being processed.");
        }

        private static string CalculatePayloadHash(
            SyncBatchOperationRequest operation)
        {
            var normalizedEntityName =
                operation.EntityName
                    .Trim()
                    .ToUpperInvariant();

            var normalizedOperation =
                operation.Operation
                    .Trim()
                    .ToUpperInvariant();

            /*
             * ServerEntityId is output data for Create.
             * It must never change the idempotency hash.
             */
            var normalizedServerEntityId =
                string.Equals(
                    normalizedOperation,
                    "CREATE",
                    StringComparison.Ordinal)
                        ? string.Empty
                        : operation.ServerEntityId?
                            .ToString("D") ??
                          string.Empty;

            var normalizedValue =
                string.Concat(
                    normalizedEntityName,
                    "\n",
                    normalizedOperation,
                    "\n",
                    operation.LocalEntityId.ToString("D"),
                    "\n",
                    normalizedServerEntityId,
                    "\n",
                    operation.Payload.GetRawText());

            var bytes =
                Encoding.UTF8.GetBytes(
                    normalizedValue);

            var hash =
                SHA256.HashData(
                    bytes);

            return Convert.ToHexString(
                hash);
        }

        private static SyncBatchItemResult NormalizeResult(
            SyncBatchOperationRequest operation,
            SyncBatchItemResult result)
        {
            result.QueueItemId =
                operation.QueueItemId;

            result.ClientOperationId =
                operation.ClientOperationId;

            result.ServerReferenceNumber =
                Truncate(
                    result.ServerReferenceNumber,
                    200);

            result.ErrorMessage =
                Truncate(
                    result.ErrorMessage,
                    2_000);

            return result;
        }

        private static SyncBatchItemResult
            CreateFailedResult(
                SyncBatchOperationRequest operation,
                string errorMessage)
        {
            return new SyncBatchItemResult
            {
                QueueItemId =
                    operation.QueueItemId,

                ClientOperationId =
                    operation.ClientOperationId,

                ServerEntityId =
                    operation.ServerEntityId,

                ServerReferenceNumber =
                    null,

                Status =
                    SyncBatchItemStatus.Failed,

                ErrorMessage =
                    Truncate(
                        errorMessage,
                        2_000)
            };
        }

        private async Task RollbackAndClearAsync(
            Microsoft.EntityFrameworkCore.Storage
                .IDbContextTransaction transaction,
            CancellationToken cancellationToken)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _db.ChangeTracker.Clear();
        }

        private static bool IsTerminalStatus(
            string? status)
        {
            return IsStatus(
                       status,
                       SyncBatchItemStatus.Done) ||
                   IsStatus(
                       status,
                       SyncBatchItemStatus.Duplicate) ||
                   IsStatus(
                       status,
                       SyncBatchItemStatus.Conflict);
        }

        private static bool IsStatus(
            string? actual,
            string expected)
        {
            return string.Equals(
                actual,
                expected,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUniqueConstraintViolation(
            DbUpdateException exception)
        {
            return exception.InnerException
                is PostgresException postgresException &&
                postgresException.SqlState ==
                    PostgresErrorCodes.UniqueViolation;
        }

        private static string? Truncate(
            string? value,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            value =
                value.Trim();

            return value.Length <= maximumLength
                ? value
                : value[..maximumLength];
        }
    }
}