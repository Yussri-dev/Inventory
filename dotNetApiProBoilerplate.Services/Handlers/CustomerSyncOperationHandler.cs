using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;

namespace Inventory.Services.Handlers
{
    public sealed class CustomerSyncOperationHandler
        : SyncOperationHandlerBase
    {
        private const string CreateOperation =
            "Create";

        private const string UpdateOperation =
            "Update";

        private const string DeleteOperation =
            "Delete";

        private readonly CustomerService _customerService;

        public CustomerSyncOperationHandler(
            CustomerService customerService)
        {
            _customerService =
                customerService;
        }

        public override string EntityName =>
            "Customer";

        protected override async Task<SyncBatchItemResult>
            ProcessCoreAsync(
                SyncBatchOperationRequest operation,
                CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (IsOperation(
                    operation,
                    CreateOperation))
            {
                return await CreateAsync(
                    operation,
                    cancellationToken);
            }

            if (IsOperation(
                    operation,
                    UpdateOperation))
            {
                return await UpdateAsync(
                    operation,
                    cancellationToken);
            }

            if (IsOperation(
                    operation,
                    DeleteOperation))
            {
                return await DeleteAsync(
                    operation,
                    cancellationToken);
            }

            return Conflict(
                operation,
                $"Unsupported Customer operation " +
                $"'{operation.Operation}'.");
        }

        private async Task<SyncBatchItemResult> CreateAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var request =
                DeserializePayload<CreateCustomerRequest>(
                    operation);

            var customer =
                await _customerService.CreateAsync(
                    request);

            return Done(
                operation,
                customer.Id);
        }

        private async Task<SyncBatchItemResult> UpdateAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var request =
                DeserializePayload<UpdateCustomerRequest>(
                    operation);

            var serverEntityId =
                GetRequiredServerEntityId(
                    operation,
                    request.Id);

            request.Id =
                serverEntityId;

            await _customerService.UpdateAsync(
                serverEntityId,
                request);

            return Done(
                operation,
                serverEntityId);
        }

        private async Task<SyncBatchItemResult> DeleteAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var serverEntityId =
                GetRequiredServerEntityId(
                    operation);

            await _customerService.DeleteAsync(
                serverEntityId);

            return Done(
                operation,
                serverEntityId);
        }

        private static Guid GetRequiredServerEntityId(
            SyncBatchOperationRequest operation,
            Guid requestId = default)
        {
            if (operation.ServerEntityId.HasValue &&
                operation.ServerEntityId.Value != Guid.Empty)
            {
                return operation.ServerEntityId.Value;
            }

            if (requestId != Guid.Empty)
            {
                return requestId;
            }

            throw new Inventory.Services.Exceptions
                .ValidationException(
                    "ServerEntityId is required for Customer " +
                    $"{operation.Operation}.");
        }
    }
}
