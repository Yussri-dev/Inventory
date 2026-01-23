using Inventory.Ui.Authentification;
using System.Net;

namespace Inventory.Ui.Services
{
    public sealed class AuthExpiredHandler : DelegatingHandler
    {
        private readonly JwtAuthStateProvider _auth;

        public AuthExpiredHandler(JwtAuthStateProvider auth)
        {
            _auth = auth;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _auth.LogoutAsync();
            }

            return response;
        }
    }
}
