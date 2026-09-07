using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Services.Abstractions;
using Inventory.Services.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceValidationException = Inventory.Services.Exceptions.ValidationException;

namespace Inventory.Services.Handlers
{
    public abstract class SyncOperationHandlerBase
       : ISyncOperationHandler
    {
        private const int MaximumErrorLength = 2_000;

        private static readonly JsonSerializerOptions
            SerializerOptions =
                CreateSerializerOptions();

        public abstract string EntityName { get; }

        public async Task<SyncBatchItemResult> ProcessAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                operation);

            try
            {
                return await ProcessCoreAsync(
                    operation,
                    cancellationToken);
            }
            catch (JsonException exception)
            {
                return Conflict(
                    operation,
                    $"Invalid {EntityName} payload: " +
                    exception.Message);
            }
            catch (ServiceValidationException exception)
            {
                return Conflict(
                    operation,
                    exception.Message);
            }
            catch (ConflictException exception)
            {
                return Conflict(
                    operation,
                    exception.Message);
            }
            catch (NotFoundException exception)
            {
                return Conflict(
                    operation,
                    exception.Message);
            }
        }

        protected abstract Task<SyncBatchItemResult>
            ProcessCoreAsync(
                SyncBatchOperationRequest operation,
                CancellationToken cancellationToken);

        protected static TRequest DeserializePayload<TRequest>(
            SyncBatchOperationRequest operation)
        {
            var request =
                operation.Payload.Deserialize<TRequest>(
                    SerializerOptions);

            if (request == null)
            {
                throw new JsonException(
                    $"Payload could not be converted to " +
                    $"'{typeof(TRequest).Name}'.");
            }

            return request;
        }

        protected static bool IsOperation(
            SyncBatchOperationRequest operation,
            string expectedOperation)
        {
            return string.Equals(
                operation.Operation?.Trim(),
                expectedOperation,
                StringComparison.OrdinalIgnoreCase);
        }

        protected static SyncBatchItemResult Done(
            SyncBatchOperationRequest operation,
            Guid? serverEntityId)
        {
            return new SyncBatchItemResult
            {
                QueueItemId =
                    operation.QueueItemId,

                ClientOperationId =
                    operation.ClientOperationId,

                ServerEntityId =
                    serverEntityId,

                Status =
                    SyncBatchItemStatus.Done,

                ErrorMessage =
                    null
            };
        }

        protected static SyncBatchItemResult Duplicate(
            SyncBatchOperationRequest operation,
            Guid? serverEntityId)
        {
            return new SyncBatchItemResult
            {
                QueueItemId =
                    operation.QueueItemId,

                ClientOperationId =
                    operation.ClientOperationId,

                ServerEntityId =
                    serverEntityId,

                Status =
                    SyncBatchItemStatus.Duplicate,

                ErrorMessage =
                    null
            };
        }

        protected static SyncBatchItemResult Conflict(
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
                    SyncBatchItemStatus.Conflict,

                ErrorMessage =
                    Truncate(
                        errorMessage)
            };
        }

        protected static SyncBatchItemResult Failed(
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
                        errorMessage)
            };
        }

        private static JsonSerializerOptions
            CreateSerializerOptions()
        {
            var options =
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                };

            options.Converters.Add(
                new JsonStringEnumConverter());

            return options;
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
