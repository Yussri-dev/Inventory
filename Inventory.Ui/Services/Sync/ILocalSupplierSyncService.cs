using Inventory.Dto.Suppliers.Results;

namespace Inventory.Ui.Services.Sync
{
    public interface ILocalSupplierSyncService
    {
        Task FullSyncAsync(
            CancellationToken cancellationToken = default);

        Task UpsertFromServerAsync(
            SupplierResult serverSupplier,
            Guid? originatingLocalId = null,
            CancellationToken cancellationToken = default);

        Task MarkDeletedFromServerAsync(
            Guid serverSupplierId,
            CancellationToken cancellationToken = default);

        Task<bool> HasInitialSyncCompletedAsync(
            CancellationToken cancellationToken = default);
    }
}
