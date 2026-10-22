using Inventory.Dto.Auth.Requests;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Inventory.Ui.Authentification;

public sealed class JwtAuthStateProvider
    : AuthenticationStateProvider
{
    private readonly ISecureStorageService _storage;
    private readonly NavigationManager _navigationManager;
    private readonly ILocalTenantContext _tenantContext;
    private readonly IAuthApiOpen _authApi;

    private readonly SemaphoreSlim _lock =
        new(1, 1);

    private Task<AuthenticationState>? _cachedStateTask;

    public JwtAuthStateProvider(
        ISecureStorageService storage,
        NavigationManager navigationManager,
        IAuthApiOpen authApi,
        ILocalTenantContext tenantContext)
    {
        _storage = storage;
        _navigationManager = navigationManager;
        _authApi = authApi;
        _tenantContext = tenantContext;
    }

    public override Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        if (_cachedStateTask != null)
        {
            return _cachedStateTask;
        }

        return FetchStateWithLockAsync();
    }

    private async Task<AuthenticationState>
        FetchStateWithLockAsync()
    {
        await _lock.WaitAsync();

        try
        {
            _cachedStateTask ??=
                FetchStateAsync();
        }
        finally
        {
            _lock.Release();
        }

        return await _cachedStateTask;
    }

    private async Task<AuthenticationState>
        FetchStateAsync()
    {
        try
        {
            var token =
                await _storage.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                _tenantContext.Clear();

                return Anonymous();
            }

            var normalizedToken =
                NormalizeBearerToken(token);

            var claims =
                JwtParser.Parse(normalizedToken)
                    .ToList();

            if (!TryGetExpiration(
                    claims,
                    out var expiresAt))
            {
                await ClearInvalidSessionAsync();

                return Anonymous();
            }

            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                var refreshedToken =
                    await TryRefreshAccessTokenAsync();

                if (string.IsNullOrWhiteSpace(refreshedToken))
                {
                    await ClearInvalidSessionAsync();

                    return Anonymous();
                }

                normalizedToken =
                    NormalizeBearerToken(refreshedToken);

                claims =
                    JwtParser.Parse(normalizedToken)
                        .ToList();

                if (!TryGetExpiration(
                        claims,
                        out expiresAt) ||
                    expiresAt <= DateTimeOffset.UtcNow)
                {
                    await ClearInvalidSessionAsync();

                    return Anonymous();
                }
            }

            RestoreTenantContext(claims);

            var identity =
                new ClaimsIdentity(
                    claims,
                    authenticationType: "jwt");

            return new AuthenticationState(
                new ClaimsPrincipal(identity));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Authentication state restoration failed: {exception}");

            await ClearInvalidSessionAsync();

            return Anonymous();
        }
    }
    private async Task<string?> TryRefreshAccessTokenAsync()
    {
        var refreshToken =
            await _storage.GetRefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        try
        {
            var result =
                await _authApi.Refresh(
                    new RefreshTokenRequest
                    {
                        RefreshToken = refreshToken
                    });

            if (string.IsNullOrWhiteSpace(result.AccessToken) ||
                string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                return null;
            }

            var accessToken =
                NormalizeBearerToken(
                    result.AccessToken);

            await _storage.SaveTokenAsync(
                accessToken);

            await _storage.SaveRefreshTokenAsync(
                result.RefreshToken);

            return accessToken;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Silent token refresh failed: {exception.Message}");

            return null;
        }
    }
    public void NotifyUserLoggedIn(
        string token)
    {
        var normalizedToken =
            NormalizeBearerToken(token);

        var claims =
            JwtParser.Parse(normalizedToken)
                .ToList();

        RestoreTenantContext(claims);

        var identity =
            new ClaimsIdentity(
                claims,
                authenticationType: "jwt");

        var state =
            new AuthenticationState(
                new ClaimsPrincipal(identity));

        _cachedStateTask =
            Task.FromResult(state);

        NotifyAuthenticationStateChanged(
            _cachedStateTask);
    }

    public void NotifyUserLoggedInOffline(
        Guid userId,
        string email,
        string? fullName,
        string role,
        Guid? tenantId)
    {
        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()),

                new(
                    ClaimTypes.Email,
                    email),

                new(
                    ClaimTypes.Name,
                    string.IsNullOrWhiteSpace(fullName)
                        ? email
                        : fullName),

                new(
                    ClaimTypes.Role,
                    role),

                new(
                    "auth_mode",
                    "offline")
            };

        if (tenantId.HasValue &&
            tenantId.Value != Guid.Empty)
        {
            claims.Add(
                new Claim(
                    "tenantId",
                    tenantId.Value.ToString()));

            _tenantContext.SetTenant(
                tenantId.Value);
        }
        else
        {
            _tenantContext.Clear();
        }

        var identity =
            new ClaimsIdentity(
                claims,
                authenticationType: "offline");

        var state =
            new AuthenticationState(
                new ClaimsPrincipal(identity));

        _cachedStateTask =
            Task.FromResult(state);

        NotifyAuthenticationStateChanged(
            _cachedStateTask);
    }

    public void NotifyUserLoggedOut()
    {
        _cachedStateTask =
            Task.FromResult(
                Anonymous());

        _tenantContext.Clear();

        NotifyAuthenticationStateChanged(
            _cachedStateTask);
    }

    public async Task LogoutAsync(
        IServiceProvider? serviceProvider = null)
    {
        await _storage.ClearAsync();

        NotifyUserLoggedOut();

        try
        {
            var navigationManager =
                serviceProvider?
                    .GetService<NavigationManager>()
                ?? _navigationManager;

            navigationManager.NavigateTo(
                "/login",
                forceLoad: true);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Logout navigation failed: {exception.Message}");
        }
    }

    private void RestoreTenantContext(
        IEnumerable<Claim> claims)
    {
        var tenantId =
            TryGetTenantId(claims);

        if (tenantId.HasValue)
        {
            _tenantContext.SetTenant(
                tenantId.Value);
        }
        else
        {
            _tenantContext.Clear();
        }
    }

    private static Guid? TryGetTenantId(
        IEnumerable<Claim> claims)
    {
        var tenantClaim =
            claims.FirstOrDefault(claim =>
                claim.Type.Equals(
                    "TenantId",
                    StringComparison.OrdinalIgnoreCase) ||

                claim.Type.Equals(
                    "tenant_id",
                    StringComparison.OrdinalIgnoreCase) ||

                claim.Type.Equals(
                    "tenantId",
                    StringComparison.OrdinalIgnoreCase));

        if (tenantClaim == null)
        {
            return null;
        }

        return Guid.TryParse(
            tenantClaim.Value,
            out var tenantId) &&
               tenantId != Guid.Empty
            ? tenantId
            : null;
    }

    private static bool TryGetExpiration(
        IEnumerable<Claim> claims,
        out DateTimeOffset expiresAt)
    {
        expiresAt = default;

        var expirationClaim =
            claims.FirstOrDefault(
                claim => claim.Type == "exp");

        if (expirationClaim == null)
        {
            return false;
        }

        if (!long.TryParse(
                expirationClaim.Value,
                out var expirationSeconds))
        {
            return false;
        }

        try
        {
            expiresAt =
                DateTimeOffset.FromUnixTimeSeconds(
                    expirationSeconds);

            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private async Task ClearInvalidSessionAsync()
    {
        try
        {
            await _storage.ClearAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Token removal failed: {exception.Message}");
        }

        _tenantContext.Clear();
        _cachedStateTask = null;
    }

    private static string NormalizeBearerToken(
        string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        return token.StartsWith(
            "Bearer ",
            StringComparison.OrdinalIgnoreCase)
            ? token["Bearer ".Length..].Trim()
            : token.Trim();
    }

    private static AuthenticationState Anonymous()
    {
        return new AuthenticationState(
            new ClaimsPrincipal(
                new ClaimsIdentity()));
    }
}