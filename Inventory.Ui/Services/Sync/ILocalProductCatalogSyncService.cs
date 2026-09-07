using Inventory.Dto.ProductCatalogs.Results;

namespace Inventory.Ui.Services.Sync
{
    public interface ILocalProductCatalogSyncService
    {
        Task FullSyncAsync(
            CancellationToken cancellationToken = default);

        Task UpsertAsync(
            ProductCatalogResult catalog,
            CancellationToken cancellationToken = default);

        Task MarkDeletedAsync(
            Guid catalogId,
            CancellationToken cancellationToken = default);

        Task<bool> HasLocalDataAsync(
            CancellationToken cancellationToken = default);
    }
}
