using Inventory.Dto.Damages.Results;

namespace Inventory.Ui.Services.Sync
{
    public interface ILocalDamageSyncService
    {
        Task FullSyncAsync(
            CancellationToken cancellationToken = default);

        Task UpsertFromServerAsync(
            DamageResult serverDamage,
            Guid? originatingLocalId = null,
            CancellationToken cancellationToken = default);

        Task<bool> HasInitialSyncCompletedAsync(
            CancellationToken cancellationToken = default);
    }
}
