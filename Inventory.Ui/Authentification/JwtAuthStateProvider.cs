

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


        private Task<AuthenticationState>? _cachedStateTask;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public JwtAuthStateProvider(
            ISecureStorageService storage,
            NavigationManager nav)
        {
            _storage = storage;
            _nav = nav;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_cachedStateTask is not null)
                return _cachedStateTask;

            return FetchStateWithLockAsync();
        }

        private async Task<AuthenticationState> FetchStateWithLockAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_cachedStateTask is null)
                    _cachedStateTask = FetchStateAsync();
            }
            finally
            {
                _lock.Release();
            }

            return await _cachedStateTask;
        }

        private async Task<AuthenticationState> FetchStateAsync()
        {
            try
            {
                var token = await _storage.GetTokenAsync();

                if (string.IsNullOrWhiteSpace(token))
                {
                    return Anonymous();
                }

                var claims = JwtParser.Parse(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch (Exception)
            {

                return Anonymous();
            }
        }

        //public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        //{
        //    try
        //    {
        //        var token = await _storage.GetTokenAsync();
        //        if (string.IsNullOrWhiteSpace(token))
        //            return Anonymous();

        //        var claims = JwtParser.Parse(token);
        //        var identity = new ClaimsIdentity(claims, "jwt");

        //        return new AuthenticationState(new ClaimsPrincipal(identity));
        //    }
        //    catch
        //    {
        //        //await _storage.RemoveTokenAsync();
        //        return Anonymous();
        //    }
        //}


        public async Task LogoutAsync(IServiceProvider? serviceProvider = null)
        {
            _cachedStateTask = null;

            await _storage.RemoveTokenAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));

            try
            {
                var nav = serviceProvider?.GetService<NavigationManager>() ?? _nav;
                nav.NavigateTo("/login", true);
            }
            catch
            {
                // NavigationManager pas encore prêt, on ignore
                // la redirection sera gérée par le composant via AuthenticationState
            }
        }

        public void NotifyUserLoggedIn(string token)
        {
            var claims = JwtParser.Parse(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var state = new AuthenticationState(new ClaimsPrincipal(identity));

            _cachedStateTask = Task.FromResult(state);

            NotifyAuthenticationStateChanged(_cachedStateTask);
        }

        private static AuthenticationState Anonymous()
            => new(new ClaimsPrincipal(new ClaimsIdentity()));
    }

}
