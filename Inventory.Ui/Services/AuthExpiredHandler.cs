using Inventory.Dto.Auth.Requests;
using Inventory.Ui.Interfaces;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;

namespace Inventory.Ui.Services
{
    public sealed class AuthExpiredHandler : DelegatingHandler
    {
        private readonly ISecureStorageService _storage;
        private readonly NavigationManager _navigation;

        public AuthExpiredHandler(
            ISecureStorageService storage,
            NavigationManager navigation)
        {
            _storage = storage;
            _navigation = navigation;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _storage.RemoveTokenAsync();

                _navigation.NavigateTo("/login", replace: true);
            }

            return response;
        }
    }
}
