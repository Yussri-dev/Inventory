

using Inventory.Ui.Interfaces;

namespace Inventory.Ui.Services
{
    public class SecureStorageService : ISecureStorageService
    {
        private const string TokenKey = "auth_token";

        public async Task SaveTokenAsync(string token)
        {
            await SecureStorage.Default.SetAsync(TokenKey, token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }

        public Task RemoveTokenAsync()
        {
            SecureStorage.Default.Remove(TokenKey);
            return Task.CompletedTask;
        }
    }
}
