using Inventory.Dto.Auth.Results;
using Inventory.LocalDB.Models;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalUserSessionService
    {
        Task SaveFromAuthResultAsync(
            AuthResult authResult,
            string plainPassword,
            CancellationToken cancellationToken = default);

        Task<LocalUserSession?> GetCurrentAsync(
            CancellationToken cancellationToken = default);

        Task<LocalUserSession?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);

        Task<LocalUserSession?> ValidateOfflineLoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default);

        Task ClearAsync(
            CancellationToken cancellationToken = default);
    }
}
