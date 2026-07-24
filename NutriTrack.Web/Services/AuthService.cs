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
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthTokensDto>()
            ?? throw new Exception("Failed to parse login response");

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
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Registration failed: {errorContent}");
        }
    }

    public async Task ConfirmEmailAsync(string token)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/confirm-email", new { token });

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Confirmation failed: {errorContent}");
        }
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
                    await LogoutAsync();
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
                if (result == null)
                {
                    await LogoutAsync();
                    return null;
                }

                await _localStorage.SetItemAsync(AccessTokenKey, result.AccessToken);
                await _localStorage.SetItemAsync(RefreshTokenKey, result.RefreshToken);

                ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.AccessToken);

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token refresh failed; logging out");
                await LogoutAsync();
                return null;
            }
        }

        return accessToken;
    }
}