using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Services.Abstractions;
using Inventory.Services.Exceptions;
using Microsoft.Extensions.Logging;

namespace Inventory.Services.Sync
{
    public sealed class SyncBatchService
         : ISyncBatchService
    {
        private const int MaximumBatchSize =
            5_000;

        private const int MaximumErrorLength =
            2_000;

        private readonly IReadOnlyDictionary<
            string,
            ISyncOperationHandler> _handlers;

        private readonly IReadOnlyDictionary<
            string,
            ISyncBatchOperationHandler> _batchHandlers;

        private readonly ISyncOperationExecutor
            _operationExecutor;

        private readonly ISyncBatchOperationExecutor
            _batchOperationExecutor;

        private readonly ILogger<SyncBatchService> _logger;

        public SyncBatchService(
            IEnumerable<ISyncOperationHandler> handlers,
            IEnumerable<ISyncBatchOperationHandler> batchHandlers,
            ISyncOperationExecutor operationExecutor,
            ISyncBatchOperationExecutor batchOperationExecutor,
            ILogger<SyncBatchService> logger)
        {
            ArgumentNullException.ThrowIfNull(
                handlers);

            ArgumentNullException.ThrowIfNull(
                batchHandlers);

            ArgumentNullException.ThrowIfNull(
                operationExecutor);

            ArgumentNullException.ThrowIfNull(
                batchOperationExecutor);

            ArgumentNullException.ThrowIfNull(
                logger);

            _operationExecutor =
                operationExecutor;

            _batchOperationExecutor =
                batchOperationExecutor;

            _logger =
                logger;

            var handlerList =
                handlers.ToList();

            var duplicateEntityNames =
                handlerList
                    .Where(handler =>
                        !string.IsNullOrWhiteSpace(
                            handler.EntityName))
                    .GroupBy(
                        handler =>
                            handler.EntityName.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group =>
                        group.Count() > 1)
                    .Select(group =>
                        group.Key)
                    .ToList();

            if (duplicateEntityNames.Count > 0)
            {
                throw new InvalidOperationException(
                    "Multiple synchronization handlers are " +
                    "registered for: " +
                    string.Join(
                        ", ",
                        duplicateEntityNames));
            }

            _handlers =
                handlerList
                    .Where(handler =>
                        !string.IsNullOrWhiteSpace(
                            handler.EntityName))
                    .ToDictionary(
                        handler =>
                            handler.EntityName.Trim(),
                        handler =>
                            handler,
                        StringComparer.OrdinalIgnoreCase);

            var batchHandlerList =
                batchHandlers.ToList();

            var duplicateBatchEntityNames =
                batchHandlerList
                    .Where(handler =>
                        !string.IsNullOrWhiteSpace(
                            handler.EntityName))
                    .GroupBy(
                        handler =>
                            handler.EntityName.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group =>
                        group.Count() > 1)
                    .Select(group =>
                        group.Key)
                    .ToList();

            if (duplicateBatchEntityNames.Count > 0)
            {
                throw new InvalidOperationException(
                    "Multiple batch synchronization handlers are " +
                    "registered for: " +
                    string.Join(
                        ", ",
                        duplicateBatchEntityNames));
            }

            _batchHandlers =
                batchHandlerList
                    .Where(handler =>
                        !string.IsNullOrWhiteSpace(
                            handler.EntityName))
                    .ToDictionary(
                        handler =>
                            handler.EntityName.Trim(),
                        handler =>
                            handler,
                        StringComparer.OrdinalIgnoreCase);
        }

        public async Task<SyncBatchResult> ProcessAsync(
            SyncBatchRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ValidateBatch(
                request);

            var result =
                new SyncBatchResult
                {
                    BatchId =
                        request.BatchId
                };

            var processedClientOperationIds =
                new HashSet<Guid>();

            var validOperations =
                new List<SyncBatchOperationRequest>(
                    request.Operations.Count);

            foreach (var operation in
                     request.Operations)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var validationError =
                    ValidateOperation(
                        operation,
                        processedClientOperationIds);

                if (validationError != null)
                {
                    result.Items.Add(
                        CreateConflictResult(
                            operation,
                            validationError));

                    continue;
                }

                validOperations.Add(
                    operation);
            }

            var operationGroups =
                validOperations
                    .GroupBy(
                        operation =>
                            operation.EntityName.Trim(),
                        StringComparer.OrdinalIgnoreCase);

            foreach (var operationGroup in operationGroups)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var entityName =
                    operationGroup.Key;

                var operations =
                    operationGroup.ToList();

                if (_batchHandlers.TryGetValue(
                        entityName,
                        out var batchHandler))
                {
                    await ProcessBatchGroupAsync(
                        request.BatchId,
                        batchHandler,
                        operations,
                        result,
                        cancellationToken);

                    continue;
                }

                if (_handlers.TryGetValue(
                        entityName,
                        out var handler))
                {
                    await ProcessLegacyGroupAsync(
                        request.BatchId,
                        handler,
                        operations,
                        result,
                        cancellationToken);

                    continue;
                }

                foreach (var operation in operations)
                {
                    result.Items.Add(
                        CreateConflictResult(
                            operation,
                            $"No synchronization handler is " +
                            $"registered for entity " +
                            $"'{entityName}'."));
                }
            }

            result.ProcessedAtUtc =
                DateTime.UtcNow;

            return result;
        }

        private async Task ProcessBatchGroupAsync(
            Guid batchId,
            ISyncBatchOperationHandler handler,
            IReadOnlyList<SyncBatchOperationRequest> operations,
            SyncBatchResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var batchResults =
                    await _batchOperationExecutor.ExecuteAsync(
                        batchId,
                        operations,
                        handler,
                        cancellationToken);

                if (batchResults == null)
                {
                    foreach (var operation in operations)
                    {
                        result.Items.Add(
                            CreateFailedResult(
                                operation,
                                "The batch synchronization handler " +
                                "returned no results."));
                    }

                    return;
                }

                var resultsByQueueItemId =
                    batchResults
                        .GroupBy(item =>
                            item.QueueItemId)
                        .ToDictionary(
                            group =>
                                group.Key,
                            group =>
                                group.ToList());

                foreach (var operation in operations)
                {
                    if (!resultsByQueueItemId.TryGetValue(
                            operation.QueueItemId,
                            out var matchingResults))
                    {
                        result.Items.Add(
                            CreateFailedResult(
                                operation,
                                "The batch synchronization handler " +
                                "returned no result for this operation."));

                        continue;
                    }

                    if (matchingResults.Count != 1)
                    {
                        result.Items.Add(
                            CreateFailedResult(
                                operation,
                                "The batch synchronization handler " +
                                "returned multiple results for the " +
                                "same queue item."));

                        continue;
                    }

                    result.Items.Add(
                        NormalizeResult(
                            operation,
                            matchingResults[0]));
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken
                    .IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Batch synchronization failed for entity " +
                    "{EntityName} with {OperationCount} operations.",
                    handler.EntityName,
                    operations.Count);

                foreach (var operation in operations)
                {
                    result.Items.Add(
                        CreateFailedResult(
                            operation,
                            exception.Message));
                }
            }
        }

        private async Task ProcessLegacyGroupAsync(
            Guid batchId,
            ISyncOperationHandler handler,
            IReadOnlyList<SyncBatchOperationRequest> operations,
            SyncBatchResult result,
            CancellationToken cancellationToken)
        {
            foreach (var operation in operations)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                try
                {
                    var itemResult =
                        await _operationExecutor.ExecuteAsync(
                            batchId,
                            operation,
                            handler,
                            cancellationToken);

                    result.Items.Add(
                        NormalizeResult(
                            operation,
                            itemResult));
                }
                catch (OperationCanceledException)
                    when (cancellationToken
                        .IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Synchronization failed for queue item " +
                        "{QueueItemId}, operation {ClientOperationId}, " +
                        "entity {EntityName}.",
                        operation.QueueItemId,
                        operation.ClientOperationId,
                        operation.EntityName);

                    result.Items.Add(
                        CreateFailedResult(
                            operation,
                            exception.Message));
                }
            }
        }

        private static void ValidateBatch(
            SyncBatchRequest request)
        {
            if (request.BatchId == Guid.Empty)
            {
                throw new ValidationException(
                    "BatchId is required.");
            }

            if (request.Operations == null ||
                request.Operations.Count == 0)
            {
                throw new ValidationException(
                    "The synchronization batch must contain " +
                    "at least one operation.");
            }

            if (request.Operations.Count >
                MaximumBatchSize)
            {
                throw new ValidationException(
                    $"The synchronization batch cannot contain " +
                    $"more than {MaximumBatchSize} operations.");
            }
        }

        private static string? ValidateOperation(
            SyncBatchOperationRequest? operation,
            ISet<Guid> processedClientOperationIds)
        {
            if (operation == null)
            {
                return
                    "The synchronization operation is required.";
            }

            if (operation.QueueItemId == Guid.Empty)
            {
                return
                    "QueueItemId is required.";
            }

            if (operation.ClientOperationId ==
                Guid.Empty)
            {
                return
                    "ClientOperationId is required.";
            }

            if (!processedClientOperationIds.Add(
                    operation.ClientOperationId))
            {
                return
                    $"ClientOperationId " +
                    $"'{operation.ClientOperationId}' appears " +
                    $"more than once in the same batch.";
            }

            if (operation.LocalEntityId == Guid.Empty)
            {
                return
                    "LocalEntityId is required.";
            }

            if (string.IsNullOrWhiteSpace(
                    operation.EntityName))
            {
                return
                    "EntityName is required.";
            }

            if (string.IsNullOrWhiteSpace(
                    operation.Operation))
            {
                return
                    "Operation is required.";
            }

            if (operation.Payload.ValueKind ==
                    System.Text.Json.JsonValueKind.Undefined ||
                operation.Payload.ValueKind ==
                    System.Text.Json.JsonValueKind.Null)
            {
                return
                    "Payload is required.";
            }

            return null;
        }

        private static SyncBatchItemResult NormalizeResult(
            SyncBatchOperationRequest operation,
            SyncBatchItemResult? itemResult)
        {
            if (itemResult == null)
            {
                return CreateFailedResult(
                    operation,
                    "The synchronization handler returned no result.");
            }

            itemResult.QueueItemId =
                operation.QueueItemId;

            itemResult.ClientOperationId =
                operation.ClientOperationId;

            if (!IsSupportedStatus(
                    itemResult.Status))
            {
                itemResult.Status =
                    SyncBatchItemStatus.Failed;

                itemResult.ErrorMessage =
                    "The synchronization handler returned an " +
                    "unsupported status.";
            }

            itemResult.ErrorMessage =
                Truncate(
                    itemResult.ErrorMessage);

            return itemResult;
        }

        private static bool IsSupportedStatus(
            string? status)
        {
            return string.Equals(
                       status,
                       SyncBatchItemStatus.Done,
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       status,
                       SyncBatchItemStatus.Duplicate,
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       status,
                       SyncBatchItemStatus.Failed,
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       status,
                       SyncBatchItemStatus.Conflict,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static SyncBatchItemResult
            CreateConflictResult(
                SyncBatchOperationRequest? operation,
                string errorMessage)
        {
            return new SyncBatchItemResult
            {
                QueueItemId =
                    operation?.QueueItemId ??
                    Guid.Empty,

                ClientOperationId =
                    operation?.ClientOperationId ??
                    Guid.Empty,

                ServerEntityId =
                    operation?.ServerEntityId,

                Status =
                    SyncBatchItemStatus.Conflict,

                ErrorMessage =
                    Truncate(
                        errorMessage)
            };
        }

        private static SyncBatchItemResult
            CreateFailedResult(
                SyncBatchOperationRequest operation,
                string? errorMessage)
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
                        string.IsNullOrWhiteSpace(
                            errorMessage)
                            ? "The synchronization operation failed."
                            : errorMessage)
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

            return value.Length <=
                   MaximumErrorLength
                ? value
                : value[..MaximumErrorLength];
        }
    }
}