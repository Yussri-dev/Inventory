using Inventory.Ui.Interfaces;
using System.Net.Http.Headers;

namespace Inventory.Ui.Services
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly ISecureStorageService _storage;

        public AuthHeaderHandler(ISecureStorageService storage)
        {
            _storage = storage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = await _storage.GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
