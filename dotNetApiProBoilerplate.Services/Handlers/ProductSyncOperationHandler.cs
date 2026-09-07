using Inventory.Dto.Products.Requests;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;

namespace Inventory.Services.Handlers
{
    public sealed class ProductSyncOperationHandler
         : SyncOperationHandlerBase
    {
        private const string CreateOperation =
            "Create";

        private const string UpdateOperation =
            "Update";

        private const string DeleteOperation =
            "Delete";

        private readonly ProductService _productService;

        public ProductSyncOperationHandler(
            ProductService productService)
        {
            _productService =
                productService;
        }

        public override string EntityName =>
            "Product";

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
                $"Unsupported Product operation " +
                $"'{operation.Operation}'.");
        }

        private async Task<SyncBatchItemResult> CreateAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var request =
                DeserializePayload<CreateProductRequest>(
                    operation);

            var product =
                await _productService.CreateAsync(
                    request);

            return Done(
                operation,
                product.Id);
        }

        private async Task<SyncBatchItemResult> UpdateAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var request =
                DeserializePayload<UpdateProductRequest>(
                    operation);

            var serverEntityId =
                GetRequiredServerEntityId(
                    operation);

            await _productService.UpdateAsync(
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

            await _productService.DeleteAsync(
                serverEntityId);

            return Done(
                operation,
                serverEntityId);
        }

        private static Guid GetRequiredServerEntityId(
            SyncBatchOperationRequest operation)
        {
            if (operation.ServerEntityId.HasValue &&
                operation.ServerEntityId.Value != Guid.Empty)
            {
                return operation.ServerEntityId.Value;
            }

            throw new Inventory.Services.Exceptions
                .ValidationException(
                    "ServerEntityId is required for Product " +
                    $"{operation.Operation}.");
        }
    }
}
