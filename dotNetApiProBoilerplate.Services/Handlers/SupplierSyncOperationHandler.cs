using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;

namespace Inventory.Services.Handlers
{
    public sealed class SupplierSyncOperationHandler
        : SyncOperationHandlerBase
    {
        private const string CreateOperation =
            "Create";

        private const string UpdateOperation =
            "Update";

        private const string DeleteOperation =
            "Delete";

        private readonly SupplierService _supplierService;

        public SupplierSyncOperationHandler(
            SupplierService supplierService)
        {
            _supplierService =
                supplierService;
        }

        public override string EntityName =>
            "Supplier";

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
                $"Unsupported Supplier operation " +
                $"'{operation.Operation}'.");
        }

        private async Task<SyncBatchItemResult> CreateAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var request =
                DeserializePayload<CreateSupplierRequest>(
                    operation);

            var supplier =
                await _supplierService.CreateAsync(
                    request);

            return Done(
                operation,
                supplier.Id);
        }

        private async Task<SyncBatchItemResult> UpdateAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var request =
                DeserializePayload<UpdateSupplierRequest>(
                    operation);

            var serverEntityId =
                GetRequiredServerEntityId(
                    operation,
                    request.Id);

            request.Id =
                serverEntityId;

            await _supplierService.UpdateAsync(
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

            await _supplierService.DeleteAsync(
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
                    "ServerEntityId is required for Supplier " +
                    $"{operation.Operation}.");
        }
    }
}
