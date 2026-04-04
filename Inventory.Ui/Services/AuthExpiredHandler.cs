using Inventory.Ui.Authentification;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace Inventory.Ui.Services
{
    public sealed class AuthExpiredHandler : DelegatingHandler
    {
        private readonly JwtAuthStateProvider _auth;
        private readonly IServiceProvider _serviceProvider;

        public AuthExpiredHandler(JwtAuthStateProvider auth, IServiceProvider serviceProvider)
        {
            _auth = auth;
            _serviceProvider = serviceProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _auth.LogoutAsync(_serviceProvider);
            }

            return response;
        }
    }
}