using Inventory.Dto.Stock.Results;

namespace Inventory.Ui.Services.Sync;

public interface ILocalStockSyncService
{
    Task FullSyncAsync(
        CancellationToken cancellationToken = default);

    Task UpsertFromServerAsync(
        StockResult serverStock,
        CancellationToken cancellationToken = default);

    Task<bool> HasInitialSyncCompletedAsync(
        CancellationToken cancellationToken = default);
}