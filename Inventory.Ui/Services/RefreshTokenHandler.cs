using Inventory.Dto.Auth.Requests;
using Inventory.Ui.Interfaces;
using System.Net;
using System.Net.Http.Headers;

namespace Inventory.Ui.Services
{
    public sealed class RefreshTokenHandler : DelegatingHandler
    {
        private readonly ISecureStorageService _storage;
        private readonly IAuthApi _authApi;

        // évite plusieurs refresh simultanés
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        public RefreshTokenHandler(
            ISecureStorageService storage,
            IAuthApi authApi)
        {
            _storage = storage;
            _authApi = authApi;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            // tout va bien
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            await _refreshLock.WaitAsync(cancellationToken);

            try
            {
                var refreshToken = await _storage.GetRefreshTokenAsync();
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return response;

                var refreshResult = await _authApi.Refresh(new RefreshTokenRequest
                {
                    RefreshToken = refreshToken
                });

                // sauvegarde nouveaux tokens
                await _storage.SaveTokenAsync(refreshResult.AccessToken);
                await _storage.SaveRefreshTokenAsync(refreshResult.RefreshToken);

                // rejouer la requête originale avec le nouveau token
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);

                return await base.SendAsync(request, cancellationToken);
            }
            catch
            {
                // échec refresh → laisser AuthExpiredHandler gérer le logout
                return response;
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }
}
