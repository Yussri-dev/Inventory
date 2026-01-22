

using Inventory.Ui.Interfaces;

namespace Inventory.Ui.Services
{
    public sealed class SecureStorageService : ISecureStorageService
    {
        private const string AccessTokenKey = "auth_access_token";
        private const string RefreshTokenKey = "auth_refresh_token";

        public async Task SaveTokenAsync(string token)
        {
            await SecureStorage.Default.SetAsync(AccessTokenKey, token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(AccessTokenKey);
        }

        public Task RemoveTokenAsync()
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            return Task.CompletedTask;
        }

        public async Task SaveRefreshTokenAsync(string refreshToken)
        {
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(RefreshTokenKey);
        }

        public Task RemoveRefreshTokenAsync()
        {
            SecureStorage.Default.Remove(RefreshTokenKey);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
            return Task.CompletedTask;
        }
    }
}
