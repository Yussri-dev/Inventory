using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Results;
using Inventory.LocalDB.Services.Sync;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync.Results;
using Microsoft.Extensions.Logging;
using Refit;
using System.Text.Json;

namespace Inventory.Ui.Services.Sync
{
    public sealed class LocalBulkSyncUploader
        : ILocalBulkSyncUploader
    {
        private const int MaximumBatchSize = 250;

        private const int MaximumBatchesPerRun = 200;

        private static readonly SemaphoreSlim SyncGate = new(1, 1);

        private readonly ISyncQueueService _syncQueueService;
        private readonly ISyncBatchApi _syncBatchApi;
        private readonly IReadOnlyDictionary<String, ILocalSyncPayloadBuilder> _payloadBuilders;
        private readonly ILogger<LocalBulkSyncUploader> _logger;

        public LocalBulkSyncUploader(
            ISyncQueueService syncQueueService,
            ISyncBatchApi syncBatchApi,
            IEnumerable<ILocalSyncPayloadBuilder> payloadBuilders,
            ILogger<LocalBulkSyncUploader> logger)
        {
            ArgumentNullException.ThrowIfNull(syncQueueService);

            ArgumentNullException.ThrowIfNull(syncBatchApi);

            ArgumentNullException.ThrowIfNull(payloadBuilders);

            ArgumentNullException.ThrowIfNull(logger);

            _syncQueueService = syncQueueService;

            _syncBatchApi = syncBatchApi;

            var builderList = payloadBuilders.Where(
                builder =>
                !string.IsNullOrWhiteSpace(builder.EntityName))
                .ToList();

            var duplicateEntityNames = builderList.GroupBy(builder => builder.EntityName.Trim(),
                StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateEntityNames.Count > 0)
            {
                throw new InvalidOperationException("Multiple local payload builders are registered for: " + string.Join(", ", duplicateEntityNames));
            }

            _payloadBuilders = builderList
            .ToDictionary(
                builder =>
                    builder.EntityName.Trim(),
                builder =>
                    builder,
                StringComparer.OrdinalIgnoreCase);

            _logger = logger;
        }

        public async Task<LocalBulkSyncResult>
            SyncPendingAsync(
                IReadOnlyCollection<string> entityNames,
                int batchSize = MaximumBatchSize,
                int maximumBatches = 20,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                entityNames);

            var normalizedEntityNames =
                entityNames
                    .Where(entityName =>
                        !string.IsNullOrWhiteSpace(
                            entityName))
                    .Select(entityName =>
                        entityName.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            if (normalizedEntityNames.Length == 0)
            {
                throw new ArgumentException(
                    "At least one entity name is required.",
                    nameof(entityNames));
            }

            batchSize =
                Math.Clamp(
                    batchSize,
                    100,
                    MaximumBatchSize);

            maximumBatches =
                Math.Clamp(
                    maximumBatches,
                    1,
                    MaximumBatchesPerRun);

            var result =
                new LocalBulkSyncResult();

            var lockAcquired =
                await SyncGate.WaitAsync(
                    0,
                    cancellationToken);

            if (!lockAcquired)
            {
                result.Messages.Add(
                    "A bulk synchronization is already running.");

                return result;
            }

            try
            {
                for (var batchNumber = 0;
                     batchNumber < maximumBatches;
                     batchNumber++)
                {
                    ClaimedSyncBatchResult? claimedBatch =
                        null;

                    try
                    {
                        _logger.LogInformation("Claiming synchronization batch. BatchSize={BatchSize}, BatchNumber={BatchNumber}.", batchSize, batchNumber + 1);


                        claimedBatch =
                            await _syncQueueService
                                .ClaimPendingBatchAsync(
                                    normalizedEntityNames,
                                    batchSize,
                                    cancellationToken);

                        _logger.LogInformation("Claimed synchronization batch {BatchId} with {ItemCount} items.", claimedBatch.BatchId, claimedBatch.Items.Count);

                        if (claimedBatch.BatchId ==
                                Guid.Empty ||
                            claimedBatch.Items.Count == 0)
                        {
                            break;
                        }

                        result.Claimed +=
                            claimedBatch.Items.Count;

                        var request = await CreateBatchRequestAsync(claimedBatch, cancellationToken);

                        _logger.LogInformation(
                            "Uploading synchronization batch {BatchId} " +
                            "containing {OperationCount} operations.",
                            claimedBatch.BatchId,
                            request.Operations.Count);

                        var serverResult =
                            await _syncBatchApi.UploadAsync(
                                request,
                                cancellationToken);

                        await _syncQueueService
                            .ApplyBatchResultAsync(
                                claimedBatch.BatchId,
                                serverResult,
                                cancellationToken);

                        ApplyStatistics(
                            result,
                            claimedBatch,
                            serverResult);

                        result.BatchesProcessed++;

                        _logger.LogInformation(
                            "Synchronization batch {BatchId} completed.",
                            claimedBatch.BatchId);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        if (claimedBatch?.BatchId !=
                            Guid.Empty)
                        {
                            await ReleaseBatchSafelyAsync(
                                claimedBatch!.BatchId,
                                "Bulk synchronization was cancelled by the user.");
                        }

                        result.WasCancelled =
                            true;

                        result.Messages.Add(
                            "Bulk synchronization was cancelled by the user.");

                        break;
                    }
                    catch (OperationCanceledException exception)
                    {
                        if (claimedBatch?.BatchId !=
                            Guid.Empty)
                        {
                            await ReleaseBatchSafelyAsync(
                                claimedBatch!.BatchId,
                                "The synchronization request timed out.");

                            result.Failed +=
                                claimedBatch.Items.Count;
                        }

                        result.Messages.Add(
                            "The synchronization request timed out.");

                        _logger.LogWarning(
                            exception,
                            "Bulk synchronization HTTP request timed out.");

                        break;
                    }
                    catch (ApiException exception)
                    {
                        if (claimedBatch?.BatchId !=
                            Guid.Empty)
                        {
                            await ReleaseBatchSafelyAsync(
                                claimedBatch!.BatchId,
                                CreateApiErrorMessage(
                                    exception));

                            result.Failed +=
                                claimedBatch.Items.Count;
                        }

                        result.Messages.Add(
                            CreateApiErrorMessage(
                                exception));

                        _logger.LogWarning(
                            exception,
                            "Bulk synchronization API request failed.");

                        /*
                         * Stop this run. ReleaseBatchAsync assigns
                         * NextAttemptAtUtc to prevent an immediate retry loop.
                         */
                        break;
                    }
                    catch (HttpRequestException exception)
                    {
                        if (claimedBatch?.BatchId !=
                            Guid.Empty)
                        {
                            await ReleaseBatchSafelyAsync(
                                claimedBatch!.BatchId,
                                "The synchronization API is unavailable.");

                            result.Failed +=
                                claimedBatch.Items.Count;
                        }

                        result.Messages.Add(
                            "The synchronization API is unavailable.");

                        _logger.LogWarning(
                            exception,
                            "Bulk synchronization stopped because " +
                            "the API is unavailable.");

                        break;
                    }
                    catch (JsonException exception)
                    {
                        if (claimedBatch?.BatchId != Guid.Empty)
                        {
                            await ReleaseBatchSafelyAsync(
                                claimedBatch!.BatchId,
                                "A queue item contains an invalid JSON payload.");

                            result.Failed +=
                                claimedBatch.Items.Count;
                        }

                        result.Messages.Add(
                            "A queue item contains an invalid JSON payload.");

                        _logger.LogError(
                            exception,
                            "A synchronization queue payload is invalid.");

                        break;
                    }
                    catch (Exception exception)
                    {
                        var detailedError =
                            GetDetailedErrorMessage(
                                exception);

                        if (claimedBatch?.BatchId !=
                            Guid.Empty)
                        {
                            await ReleaseBatchSafelyAsync(
                                claimedBatch!.BatchId,
                                detailedError);

                            result.Failed +=
                                claimedBatch.Items.Count;
                        }

                        result.Messages.Add(
                            detailedError);

                        _logger.LogError(
                            exception,
                            "Bulk synchronization failed.");

                        break;
                    }
                }
            }
            finally
            {
                SyncGate.Release();
            }

            return result;
        }

        private static string GetDetailedErrorMessage(Exception exception)
        {
            var messages =
                new List<string>();

            Exception? current =
                exception;

            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(
                        current.Message) &&
                    !messages.Contains(
                        current.Message,
                        StringComparer.Ordinal))
                {
                    messages.Add(
                        current.Message.Trim());
                }

                current =
                    current.InnerException;
            }

            return messages.Count == 0
                ? "Bulk synchronization failed."
                : string.Join(
                    " --> ",
                    messages);
        }

        private async Task<SyncBatchRequest>
    CreateBatchRequestAsync(
        ClaimedSyncBatchResult claimedBatch,
        CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                claimedBatch);

            var operations =
                new List<SyncBatchOperationRequest>(
                    claimedBatch.Items.Count);

            foreach (var queueItem in claimedBatch.Items)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var entityName =
                    queueItem.EntityName?
                        .Trim()
                    ?? string.Empty;

                var payloadJson =
                    queueItem.PayloadJson;

                /*
                 * Sale and future complex aggregates rebuild their payload
                 * from the current SQLite data.
                 *
                 * Existing entities continue using their stored PayloadJson.
                 */
                if (_payloadBuilders.TryGetValue(
                        entityName,
                        out var payloadBuilder))
                {
                    payloadJson =
                        await payloadBuilder
                            .BuildPayloadJsonAsync(
                                queueItem,
                                cancellationToken);
                }

                operations.Add(
                    CreateOperation(
                        queueItem,
                        payloadJson));
            }

            return new SyncBatchRequest
            {
                BatchId =
                    claimedBatch.BatchId,

                Operations =
                    operations
            };
        }

        private static SyncBatchOperationRequest CreateOperation(
    SyncQueueItem queueItem,
    string payloadJson)
        {
            ArgumentNullException.ThrowIfNull(
                queueItem);

            if (string.IsNullOrWhiteSpace(
                    payloadJson))
            {
                throw new JsonException(
                    $"Queue item '{queueItem.Id}' " +
                    $"({queueItem.EntityName}/{queueItem.Operation}) " +
                    "contains an empty JSON payload.");
            }

            JsonElement payload;

            try
            {
                using var document =
                    JsonDocument.Parse(
                        payloadJson);

                payload =
                    document.RootElement.Clone();
            }
            catch (JsonException exception)
            {
                throw new JsonException(
                    $"Queue item '{queueItem.Id}' " +
                    $"({queueItem.EntityName}/{queueItem.Operation}) " +
                    $"contains invalid JSON: {exception.Message}",
                    exception);
            }

            return new SyncBatchOperationRequest
            {
                QueueItemId =
                    queueItem.Id,

                ClientOperationId =
                    queueItem.ClientOperationId,

                LocalEntityId =
                    queueItem.LocalEntityId,

                ServerEntityId = string.Equals(queueItem.Operation, SyncOperation.Create, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : queueItem.ServerEntityId,

                EntityName =
                    queueItem.EntityName,

                Operation =
                    queueItem.Operation,

                Payload =
                    payload,

                CreatedAtUtc =
                    queueItem.CreatedAtUtc
            };
        }

        private static void ApplyStatistics(
            LocalBulkSyncResult localResult,
            ClaimedSyncBatchResult claimedBatch,
            SyncBatchResult serverResult)
        {
            var returnedQueueItemIds =
                serverResult.Items
                    .Select(item =>
                        item.QueueItemId)
                    .ToHashSet();

            foreach (var item in serverResult.Items)
            {
                if (string.Equals(
                        item.Status,
                        SyncBatchItemStatus.Done,
                        StringComparison.OrdinalIgnoreCase))
                {
                    localResult.Done++;
                }
                else if (string.Equals(
                             item.Status,
                             SyncBatchItemStatus.Duplicate,
                             StringComparison.OrdinalIgnoreCase))
                {
                    localResult.Duplicates++;
                }
                else if (string.Equals(
                             item.Status,
                             SyncBatchItemStatus.Conflict,
                             StringComparison.OrdinalIgnoreCase))
                {
                    localResult.Conflicts++;
                }
                else
                {
                    localResult.Failed++;
                }
            }

            localResult.Failed +=
                claimedBatch.Items.Count(item =>
                    !returnedQueueItemIds.Contains(
                        item.Id));
        }

        private async Task ReleaseBatchSafelyAsync(
            Guid batchId,
            string errorMessage)
        {
            try
            {
                /*
                 * CancellationToken.None is intentional. A cancelled
                 * network operation must still release the SQLite claim.
                 */
                await _syncQueueService.ReleaseBatchAsync(
                    batchId,
                    errorMessage,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Could not release synchronization batch {BatchId}.",
                    batchId);
            }
        }

        private static string CreateApiErrorMessage(
            ApiException exception)
        {
            if (!string.IsNullOrWhiteSpace(
                    exception.Content))
            {
                return
                    $"Bulk synchronization API error " +
                    $"{(int)exception.StatusCode}: " +
                    exception.Content;
            }

            return
                $"Bulk synchronization API error " +
                $"{(int)exception.StatusCode}.";
        }
    }
}