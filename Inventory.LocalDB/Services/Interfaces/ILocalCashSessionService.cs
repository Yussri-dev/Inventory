using Inventory.LocalDB.Models;


namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalCashSessionService
    {
        Task<bool> HasOpenSessionAsync(CancellationToken cancellationToken = default);

        Task<LocalCashSession?> GetOpenSessionAsync(CancellationToken cancellationToken = default);

        Task<LocalCashSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<List<LocalCashSession>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default);

        Task<LocalCashSession> OpenAsync(
            decimal openingAmount,
            Guid? openedByUserId = null,
            string? openingNotes = null,
            CancellationToken cancellationToken = default);

        Task<LocalCashSession> CloseAsync(
            Guid localCashSessionId,
            decimal countedAmount,
            Guid? closedByUserId = null,
            string? closingNotes = null,
            CancellationToken cancellationToken = default);
    }
}
