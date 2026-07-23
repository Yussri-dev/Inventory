using Inventory.Dto.Customers.Results;

namespace Inventory.Ui.Services.Sync;

public interface ILocalCustomerSyncService
{
    Task FullSyncAsync(
        CancellationToken cancellationToken = default);

    Task UpsertFromServerAsync(
        CustomerResult serverCustomer,
        Guid? originatingLocalId = null,
        CancellationToken cancellationToken = default);

    Task MarkDeletedFromServerAsync(
        Guid serverCustomerId,
        CancellationToken cancellationToken = default);

    Task<bool> HasInitialSyncCompletedAsync(
        CancellationToken cancellationToken = default);
}