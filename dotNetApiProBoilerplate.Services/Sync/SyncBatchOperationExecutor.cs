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
    public sealed class SyncBatchOperationExecutor
        : ISyncBatchOperationExecutor
    {
        private const string ProcessingStatus =
            "Processing";

        private const int MaximumBatchSize =
            5_000;

        private const int MaximumConcurrencyRetries =
            2;

        private const int MaximumErrorLength =
            2_000;

        private readonly InventoryDbContext _db;
        private readonly ITenantContext _tenantContext;

        public SyncBatchOperationExecutor(
            InventoryDbContext db,
            ITenantContext tenantContext)
        {
            _db =
                db;

            _tenantContext =
                tenantContext;
        }

        public async Task<IReadOnlyList<SyncBatchItemResult>>
            ExecuteAsync(
                Guid batchId,
                IReadOnlyList<SyncBatchOperationRequest> operations,
                ISyncBatchOperationHandler handler,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                operations);

            ArgumentNullException.ThrowIfNull(
                handler);

            if (batchId == Guid.Empty)
            {
                throw new ArgumentException(
                    "BatchId is required.",
                    nameof(batchId));
            }

            if (operations.Count == 0)
            {
                return Array.Empty<SyncBatchItemResult>();
            }

            if (operations.Count > MaximumBatchSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(operations),
                    $"A synchronization batch cannot contain more " +
                    $"than {MaximumBatchSize} operations.");
            }

            ValidateOperations(
                operations,
                handler);

            for (var attempt = 0;
                 ;
                 attempt++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                _db.ChangeTracker.Clear();

                var executionResult =
                    await TryExecuteAsync(
                        batchId,
                        operations,
                        handler,
                        attempt,
                        cancellationToken);

                if (executionResult != null)
                {
                    return executionResult;
                }
            }
        }

        private async Task<IReadOnlyList<SyncBatchItemResult>?>
            TryExecuteAsync(
                Guid batchId,
                IReadOnlyList<SyncBatchOperationRequest> operations,
                ISyncBatchOperationHandler handler,
                int attempt,
                CancellationToken cancellationToken)
        {
            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            var states =
                operations
                    .Select(operation =>
                        new OperationState(
                            operation,
                            CalculatePayloadHash(
                                operation)))
                    .ToArray();

            var clientOperationIds =
                states
                    .Select(state =>
                        state.Operation.ClientOperationId)
                    .Distinct()
                    .ToArray();

            var existingRecords =
                await _db.SyncOperationRecords
                    .AsNoTracking()
                    .Where(record =>
                        record.TenantId == tenantId &&
                        !record.IsDeleted &&
                        clientOperationIds.Contains(
                            record.ClientOperationId))
                    .ToListAsync(
                        cancellationToken);

            var existingByClientOperationId =
                existingRecords.ToDictionary(
                    record =>
                        record.ClientOperationId);

            var results =
                new Dictionary<Guid, SyncBatchItemResult>();

            var pendingStates =
                new List<OperationState>();

            foreach (var state in states)
            {
                if (existingByClientOperationId.TryGetValue(
                        state.Operation.ClientOperationId,
                        out var existingRecord))
                {
                    results[state.Operation.QueueItemId] =
                        CreateExistingResult(
                            state.Operation,
                            existingRecord,
                            state.PayloadHash);

                    continue;
                }

                pendingStates.Add(
                    state);
            }

            if (pendingStates.Count == 0)
            {
                return OrderResults(
                    operations,
                    results);
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken);

            try
            {
                var receivedAtUtc =
                    DateTime.UtcNow;

                var recordsByQueueItemId =
                    new Dictionary<Guid, SyncOperationRecord>();

                foreach (var state in pendingStates)
                {
                    var operation =
                        state.Operation;

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

                            EntityName =
                                operation.EntityName.Trim(),

                            Operation =
                                operation.Operation.Trim(),

                            Status =
                                ProcessingStatus,

                            PayloadHash =
                                state.PayloadHash,

                            ErrorMessage =
                                null,

                            ReceivedAtUtc =
                                receivedAtUtc,

                            ProcessedAtUtc =
                                null,

                            CreatedAt =
                                receivedAtUtc,

                            CreatedByUserId =
                                userId,

                            IsDeleted =
                                false
                        };

                    recordsByQueueItemId.Add(
                        operation.QueueItemId,
                        record);

                    _db.SyncOperationRecords.Add(
                        record);
                }

                /*
                 * Une seule écriture pour acquérir les verrous uniques
                 * TenantId + ClientOperationId.
                 */
                await _db.SaveChangesAsync(
                    cancellationToken);

                var pendingOperations =
                    pendingStates
                        .Select(state =>
                            state.Operation)
                        .ToArray();

                var handlerResults =
                    await handler.ProcessBatchAsync(
                        pendingOperations,
                        cancellationToken);

                var returnedResults =
                    (handlerResults ??
                     Array.Empty<SyncBatchItemResult>())
                    .Where(result =>
                        result != null)
                    .GroupBy(result =>
                        result.QueueItemId)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.ToArray());

                var processedAtUtc =
                    DateTime.UtcNow;

                foreach (var state in pendingStates)
                {
                    var operation =
                        state.Operation;

                    var record =
                        recordsByQueueItemId[
                            operation.QueueItemId];

                    SyncBatchItemResult itemResult;

                    if (!returnedResults.TryGetValue(
                            operation.QueueItemId,
                            out var matchingResults))
                    {
                        itemResult =
                            CreateFailedResult(
                                operation,
                                "The batch synchronization handler " +
                                "returned no result for this operation.");
                    }
                    else if (matchingResults.Length != 1)
                    {
                        itemResult =
                            CreateFailedResult(
                                operation,
                                "The batch synchronization handler " +
                                "returned multiple results for this operation.");
                    }
                    else
                    {
                        itemResult =
                            NormalizeResult(
                                operation,
                                matchingResults[0]);
                    }

                    if (IsStatus(
                            itemResult.Status,
                            SyncBatchItemStatus.Failed))
                    {
                        /*
                         * Failed est temporaire. Aucun enregistrement
                         * d’idempotence ne doit être conservé.
                         */
                        _db.SyncOperationRecords.Remove(
                            record);

                        results[operation.QueueItemId] =
                            itemResult;

                        continue;
                    }

                    if (!IsTerminalStatus(
                            itemResult.Status))
                    {
                        _db.SyncOperationRecords.Remove(
                            record);

                        results[operation.QueueItemId] =
                            CreateFailedResult(
                                operation,
                                "The batch synchronization handler " +
                                "returned an unsupported status.");

                        continue;
                    }

                    record.ServerEntityId =
                        itemResult.ServerEntityId ??
                        operation.ServerEntityId;

                    record.Status =
                        itemResult.Status;

                    record.ErrorMessage =
                        Truncate(
                            itemResult.ErrorMessage);

                    record.ProcessedAtUtc =
                        processedAtUtc;

                    record.ModifiedAt =
                        processedAtUtc;

                    record.ModifiedByUserId =
                        userId;

                    results[operation.QueueItemId] =
                        itemResult;
                }

                /*
                 * Cette écriture persiste les modifications métier du
                 * handler et finalise les SyncOperationRecords.
                 */
                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                return OrderResults(
                    operations,
                    results);
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(
                          exception) &&
                      attempt < MaximumConcurrencyRetries)
            {
                /*
                 * Une autre requête a gagné la course sur un
                 * ClientOperationId. Tout le batch est relu.
                 */
                await RollbackAndClearAsync(
                    transaction,
                    cancellationToken);

                return null;
            }
            catch
            {
                await RollbackAndClearAsync(
                    transaction,
                    cancellationToken);

                throw;
            }
        }

        private static void ValidateOperations(
            IReadOnlyList<SyncBatchOperationRequest> operations,
            ISyncBatchOperationHandler handler)
        {
            if (string.IsNullOrWhiteSpace(
                    handler.EntityName))
            {
                throw new InvalidOperationException(
                    "The batch handler EntityName is required.");
            }

            var queueItemIds =
                new HashSet<Guid>();

            var clientOperationIds =
                new HashSet<Guid>();

            foreach (var operation in operations)
            {
                if (operation == null)
                {
                    throw new ArgumentException(
                        "A synchronization operation cannot be null.",
                        nameof(operations));
                }

                if (operation.QueueItemId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "QueueItemId is required.",
                        nameof(operations));
                }

                if (operation.ClientOperationId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "ClientOperationId is required.",
                        nameof(operations));
                }

                if (operation.LocalEntityId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "LocalEntityId is required.",
                        nameof(operations));
                }

                if (string.IsNullOrWhiteSpace(
                        operation.EntityName))
                {
                    throw new ArgumentException(
                        "EntityName is required.",
                        nameof(operations));
                }

                if (!string.Equals(
                        operation.EntityName.Trim(),
                        handler.EntityName.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"Operation entity '{operation.EntityName}' " +
                        $"cannot be processed by handler " +
                        $"'{handler.EntityName}'.",
                        nameof(operations));
                }

                if (string.IsNullOrWhiteSpace(
                        operation.Operation))
                {
                    throw new ArgumentException(
                        "Operation is required.",
                        nameof(operations));
                }

                if (operation.Payload.ValueKind ==
                        System.Text.Json.JsonValueKind.Undefined ||
                    operation.Payload.ValueKind ==
                        System.Text.Json.JsonValueKind.Null)
                {
                    throw new ArgumentException(
                        "Payload is required.",
                        nameof(operations));
                }

                if (!queueItemIds.Add(
                        operation.QueueItemId))
                {
                    throw new ArgumentException(
                        $"QueueItemId '{operation.QueueItemId}' " +
                        "is duplicated in the batch.",
                        nameof(operations));
                }

                if (!clientOperationIds.Add(
                        operation.ClientOperationId))
                {
                    throw new ArgumentException(
                        $"ClientOperationId " +
                        $"'{operation.ClientOperationId}' " +
                        "is duplicated in the batch.",
                        nameof(operations));
                }
            }
        }

        private static string CalculatePayloadHash(
            SyncBatchOperationRequest operation)
        {
            /*
             * Ce format doit rester identique à SyncOperationExecutor.
             */
            var normalizedValue =
                string.Concat(
                    operation.EntityName
                        .Trim()
                        .ToUpperInvariant(),
                    "\n",
                    operation.Operation
                        .Trim()
                        .ToUpperInvariant(),
                    "\n",
                    operation.LocalEntityId.ToString("D"),
                    "\n",
                    operation.ServerEntityId?.ToString("D") ??
                    string.Empty,
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

        private static SyncBatchItemResult CreateExistingResult(
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

        private static SyncBatchItemResult NormalizeResult(
            SyncBatchOperationRequest operation,
            SyncBatchItemResult result)
        {
            result.QueueItemId =
                operation.QueueItemId;

            result.ClientOperationId =
                operation.ClientOperationId;

            result.ErrorMessage =
                Truncate(
                    result.ErrorMessage);

            return result;
        }

        private static IReadOnlyList<SyncBatchItemResult>
            OrderResults(
                IReadOnlyList<SyncBatchOperationRequest> operations,
                IReadOnlyDictionary<Guid, SyncBatchItemResult> results)
        {
            return operations
                .Select(operation =>
                    results.TryGetValue(
                        operation.QueueItemId,
                        out var result)
                        ? result
                        : CreateFailedResult(
                            operation,
                            "No synchronization result was produced."))
                .ToArray();
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

        private static SyncBatchItemResult CreateFailedResult(
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

                Status =
                    SyncBatchItemStatus.Failed,

                ErrorMessage =
                    Truncate(
                        errorMessage)
            };
        }

        private static string? Truncate(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            value =
                value.Trim();

            return value.Length <= MaximumErrorLength
                ? value
                : value[..MaximumErrorLength];
        }

        private sealed record OperationState(
            SyncBatchOperationRequest Operation,
            string PayloadHash);
    }
}