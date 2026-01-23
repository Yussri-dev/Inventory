

using Inventory.Ui.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Inventory.Ui.Authentification
{
    public sealed class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ISecureStorageService _storage;
        private readonly NavigationManager _nav;

        public JwtAuthStateProvider(
            ISecureStorageService storage,
            NavigationManager nav)
        {
            _storage = storage;
            _nav = nav;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _storage.GetTokenAsync();
                if (string.IsNullOrWhiteSpace(token))
                    return Anonymous();

                var claims = JwtParser.Parse(token);
                var identity = new ClaimsIdentity(claims, "jwt");

                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                await _storage.RemoveTokenAsync();
                return Anonymous();
            }
        }


        public async Task LogoutAsync()
        {
            await _storage.RemoveTokenAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
            _nav.NavigateTo("/login", true);
        }

        private static AuthenticationState Anonymous()
            => new(new ClaimsPrincipal(new ClaimsIdentity()));
    }

}
