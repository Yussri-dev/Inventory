using Inventory.Dto.Auth.Requests;
using Inventory.Ui.Interfaces;
using System.Net;
using System.Net.Http.Headers;

namespace Inventory.Ui.Services
{
    public sealed class RefreshTokenHandler : DelegatingHandler
    {
        private readonly ISecureStorageService _storage;
        private readonly IAuthApiOpen _authApi;

        private static readonly SemaphoreSlim _refreshLock =
            new(1, 1);

        public RefreshTokenHandler(
            ISecureStorageService storage,
            IAuthApiOpen authApi)
        {
            _storage = storage;
            _authApi = authApi;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var accessTokenBeforeRequest =
                await _storage.GetTokenAsync();

            using var retryRequest =
                await CloneRequestAsync(
                    request,
                    cancellationToken);

            var response =
                await base.SendAsync(
                    request,
                    cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            await _refreshLock.WaitAsync(cancellationToken);

            try
            {
                var currentAccessToken =
                    await _storage.GetTokenAsync();

                // Un autre appel a déjà effectué le refresh
                // pendant que cette requête attendait le lock.
                if (!string.IsNullOrWhiteSpace(currentAccessToken) &&
                    !string.Equals(
                        currentAccessToken,
                        accessTokenBeforeRequest,
                        StringComparison.Ordinal))
                {
                    retryRequest.Headers.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            currentAccessToken);

                    var retryResponse =
                        await base.SendAsync(
                            retryRequest,
                            cancellationToken);

                    response.Dispose();

                    return retryResponse;
                }

                var refreshToken =
                    await _storage.GetRefreshTokenAsync();

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return response;
                }

                var refreshResult =
                    await _authApi.Refresh(
                        new RefreshTokenRequest
                        {
                            RefreshToken = refreshToken
                        },
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(
                        refreshResult.AccessToken) ||
                    string.IsNullOrWhiteSpace(
                        refreshResult.RefreshToken))
                {
                    return response;
                }

                await _storage.SaveTokenAsync(
                    refreshResult.AccessToken);

                await _storage.SaveRefreshTokenAsync(
                    refreshResult.RefreshToken);

                retryRequest.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        refreshResult.AccessToken);

                var refreshedResponse =
                    await base.SendAsync(
                        retryRequest,
                        cancellationToken);

                response.Dispose();

                return refreshedResponse;
            }
            catch
            {
                // Le 401 original remonte vers AuthExpiredHandler,
                // qui pourra gérer la déconnexion.
                return response;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var clone =
                new HttpRequestMessage(
                    request.Method,
                    request.RequestUri)
                {
                    Version = request.Version,
                    VersionPolicy = request.VersionPolicy
                };

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }

            if (request.Content is not null)
            {
                var contentBytes =
                    await request.Content.ReadAsByteArrayAsync(
                        cancellationToken);

                clone.Content =
                    new ByteArrayContent(contentBytes);

                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(
                        header.Key,
                        header.Value);
                }
            }

            foreach (var option in request.Options)
            {
                clone.Options.TryAdd(
                    option.Key,
                    option.Value);
            }

            return clone;
        }
    }
}