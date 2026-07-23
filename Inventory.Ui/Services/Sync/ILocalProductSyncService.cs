using Inventory.Dto.Products.Results;

namespace Inventory.Ui.Services.Sync;

public interface ILocalProductSyncService
{
    Task FullSyncAsync(
        CancellationToken cancellationToken = default);

    Task UpsertFromServerAsync(
        ProductResult serverProduct,
        Guid? originatingLocalId = null,
        CancellationToken cancellationToken = default);

    Task MarkDeletedFromServerAsync(
        Guid serverProductId,
        CancellationToken cancellationToken = default);

    Task<bool> HasInitialSyncCompletedAsync(
        CancellationToken cancellationToken = default);
}