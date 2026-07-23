using Inventory.LocalDB.Services.Results;

namespace Inventory.LocalDB.Services.Interfaces;

public interface ILocalDamageService
{
    Task<IReadOnlyList<LocalDamageProductResult>>
        SearchProductsAsync(
            string search,
            int take = 10,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalDamageDraftResult>>
        GetDraftsAsync(
            CancellationToken cancellationToken = default);

    Task<LocalDamageDraftResult> AddDraftAsync(
        Guid productLocalId,
        decimal quantity,
        string? reason,
        CancellationToken cancellationToken = default);

    Task RemoveDraftAsync(
        Guid localDamageId,
        CancellationToken cancellationToken = default);

    Task<int> ValidateAllDraftsAsync(
        CancellationToken cancellationToken = default);
}