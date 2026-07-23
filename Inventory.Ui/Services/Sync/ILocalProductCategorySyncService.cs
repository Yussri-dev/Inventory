using Inventory.Dto.ProductCategory.Results;

namespace Inventory.Ui.Services.Sync
{
    public interface ILocalProductCategorySyncService
    {
        Task FullSyncAsync(
            CancellationToken cancellationToken = default);

        Task UpsertAsync(
            ProductCategoryResult category,
            CancellationToken cancellationToken = default);

        Task MarkDeletedAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);
    }
}
