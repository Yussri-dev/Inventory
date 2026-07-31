using Inventory.LocalDB.Models;
namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalInventorySessionService
    {
        Task<IReadOnlyList<LocalInventorySession>>
            GetAllAsync(
                CancellationToken cancellationToken = default);

        Task<LocalInventorySession?>
            GetByIdAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LocalInventoryLine>>
            GetLinesAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default);

        Task<LocalInventorySession>
            CreateAsync(
                string sessionNumber,
                string? notes = null,
                CancellationToken cancellationToken = default);

        Task<LocalInventoryLine>
            AddLineAsync(
                Guid sessionId,
                Guid productLocalId,
                decimal countedQuantity,
                string? notes = null,
                CancellationToken cancellationToken = default);

        Task<LocalInventoryLine>
            UpdateLineAsync(
                Guid lineId,
                decimal countedQuantity,
                string? notes = null,
                CancellationToken cancellationToken = default);

        Task CloseAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task ValidateAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task DeleteLineAsync(
            Guid lineId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);
    }
}
