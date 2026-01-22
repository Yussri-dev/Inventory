
namespace Inventory.Ui.Interfaces
{
    public interface ISecureStorageService
    {
        Task SaveTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();

        Task SaveRefreshTokenAsync(string refreshToken);
        Task<string?> GetRefreshTokenAsync();
        Task RemoveRefreshTokenAsync();

        Task ClearAsync();
    }
}
