using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using NutriTrack.Shared.Models.Auth;
using NutriTrack.Shared.Services;
using System.Net.Http.Json;

namespace NutriTrack.Web.Services;

/// <summary>
/// Handles all authentication operations: login, register, logout, token management.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthService> _logger;

    // LocalStorage keys
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";

    /// <summary>
    /// Serialises token refresh. Calls that fire together — the dashboard requests meal summary
    /// and history at once — used to each see the expired token and refresh independently,
    /// leaving two live rotation chains where only the last one written to localStorage was
    /// usable. The server now treats replaying a rotated token as theft, so overlapping
    /// refreshes would look like an attack and sign the user out.
    /// </summary>
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(
        HttpClient http,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthService> logger)
    {
        _http = http;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public async Task LoginAsync(string email, string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await _http.PostAsJsonAsync("/api/auth/login", request);

        if (!response.IsSuccessStatusCode)
            throw await response.ToApiExceptionAsync("Login failed. Please try again.");

        var result = await response.Content.ReadFromJsonAsync<AuthTokensDto>()
            ?? throw new ApiException(
                (int)response.StatusCode, "The server returned an empty login response.");

        await _localStorage.SetItemAsync(AccessTokenKey, result.AccessToken);
        await _localStorage.SetItemAsync(RefreshTokenKey, result.RefreshToken);

        ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.AccessToken);
    }

    public async Task RegisterAsync(string email, string password)
    {
        var request = new RegisterRequest
        {
            Email = email,
            Password = password
        };

        var response = await _http.PostAsJsonAsync("/api/auth/register", request);

        if (!response.IsSuccessStatusCode)
            throw await response.ToApiExceptionAsync("Registration failed. Please try again.");
    }

    public async Task ConfirmEmailAsync(string token)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/confirm-email", new { token });

        if (!response.IsSuccessStatusCode)
            throw await response.ToApiExceptionAsync(
                "Email confirmation failed. Please try again.");
    }

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenKey);
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var accessToken = await _localStorage.GetItemAsync<string>(AccessTokenKey);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    _http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    await _http.PostAsJsonAsync("/api/auth/revoke-token", new { refreshToken });
                }
            }
        }
        catch (Exception ex)
        {
            // Ignore errors - we're logging out anyway, but record why the revoke failed.
            _logger.LogWarning(ex, "Failed to revoke refresh token during logout");
        }

        await _localStorage.RemoveItemAsync(AccessTokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);

        _http.DefaultRequestHeaders.Authorization = null;

        ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(AccessTokenKey);
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var accessToken = await _localStorage.GetItemAsync<string>(AccessTokenKey);

        if (string.IsNullOrEmpty(accessToken))
            return null;

        if (JwtParser.IsExpired(accessToken, TimeSpan.FromMinutes(1)))
        {
            await _refreshLock.WaitAsync();
            try
            {
                // Re-read after acquiring: whoever held the lock may have already refreshed, in
                // which case the stored token is fresh and rotating again would strand a chain.
                accessToken = await _localStorage.GetItemAsync<string>(AccessTokenKey);

                if (string.IsNullOrEmpty(accessToken))
                    return null;

                if (!JwtParser.IsExpired(accessToken, TimeSpan.FromMinutes(1)))
                    return accessToken;

                return await RefreshAccessTokenAsync(accessToken);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        return accessToken;
    }

    /// <summary>
    /// Exchanges the stored refresh token. Callers must hold <see cref="_refreshLock"/>.
    /// </summary>
    private async Task<string?> RefreshAccessTokenAsync(string accessToken)
    {
        var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenKey);

        if (string.IsNullOrEmpty(refreshToken))
        {
            await LogoutAsync();
            return null;
        }

        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/refresh-token",
                new { refreshToken });

            if (!response.IsSuccessStatusCode)
            {
                // Only a 401 means the refresh token itself is dead. A 5xx or a gateway
                // hiccup is transient — tearing down the session over one would sign the
                // user out for no reason. The access token is still valid for up to a
                // minute, so hand it back and let the next call retry the refresh.
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("Refresh token rejected; logging out");
                    await LogoutAsync();
                    return null;
                }

                _logger.LogWarning(
                    "Token refresh failed with {StatusCode}; keeping the session",
                    (int)response.StatusCode);
                return accessToken;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
            if (result == null)
            {
                _logger.LogWarning("Token refresh returned an empty body; keeping the session");
                return accessToken;
            }

            await _localStorage.SetItemAsync(AccessTokenKey, result.AccessToken);
            await _localStorage.SetItemAsync(RefreshTokenKey, result.RefreshToken);

            ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.AccessToken);

            return result.AccessToken;
        }
        catch (Exception ex)
        {
            // Transport failure (offline, DNS, CORS). Same reasoning as a 5xx above.
            _logger.LogWarning(ex, "Token refresh could not reach the server; keeping the session");
            return accessToken;
        }
    }
}