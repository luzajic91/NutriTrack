using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.Logging;
using NutriTrack.Shared.Models.Auth;
using NutriTrack.Shared.Services;
using System.Net.Http.Json;

namespace NutriTrack.Web.Services;

/// <summary>
/// Handles all authentication operations: login, register, logout, token management.
///
/// The refresh token is never seen here. The server delivers it as an HttpOnly cookie, which the
/// browser attaches to auth requests and no script can read; the access token is held in memory
/// by <see cref="TokenStore"/>. Nothing authentication-related is written to localStorage, so an
/// XSS has nothing durable to steal.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokens;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Serialises token refresh. Calls that fire together — the dashboard requests meal summary
    /// and history at once — used to each see the expired token and refresh independently,
    /// leaving two live rotation chains where only one was usable. The server treats replaying a
    /// rotated token as theft, so overlapping refreshes would look like an attack.
    /// </summary>
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>
    /// A page load starts with an empty <see cref="TokenStore"/>, so the first caller tries to
    /// rebuild the session from the cookie. Attempted once: with no cookie, every guarded page
    /// would otherwise retry forever.
    /// </summary>
    private bool _restoreAttempted;

    public AuthService(HttpClient http, TokenStore tokens, ILogger<AuthService> logger)
    {
        _http = http;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task LoginAsync(string email, string password)
    {
        var response = await SendWithCredentialsAsync(
            "/api/auth/login", new LoginRequest { Email = email, Password = password });

        if (!response.IsSuccessStatusCode)
            throw await response.ToApiExceptionAsync("Login failed. Please try again.");

        var result = await response.Content.ReadFromJsonAsync<AccessTokenDto>()
            ?? throw new ApiException(
                (int)response.StatusCode, "The server returned an empty login response.");

        // The refresh token is not in this response — it arrived as a Set-Cookie header.
        _tokens.Set(result.AccessToken);
        _restoreAttempted = true;
    }

    public async Task RegisterAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest { Email = email, Password = password });

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
            // No body and no bearer token: the endpoint identifies the session from the cookie,
            // which is what lets logout work after a reload that has not refreshed yet.
            await SendWithCredentialsAsync<object>("/api/auth/revoke-token", body: null);
        }
        catch (Exception ex)
        {
            // Ignore errors - we're logging out anyway, but record why the revoke failed.
            _logger.LogWarning(ex, "Failed to revoke refresh token during logout");
        }

        // Set before clearing: Clear raises Changed synchronously, and a handler that saw
        // _restoreAttempted still false could kick off a refresh for the session just ended.
        _restoreAttempted = true;
        _tokens.Clear();
    }

    public async Task<bool> IsAuthenticatedAsync() =>
        await GetAccessTokenAsync() is not null;

    /// <summary>
    /// Rebuilds the session from the refresh cookie after a page load, when the access token
    /// held in memory is gone but the cookie may still be valid.
    /// </summary>
    public async Task<bool> TryRestoreSessionAsync()
    {
        if (_tokens.HasToken)
            return true;

        await _refreshLock.WaitAsync();
        try
        {
            if (_tokens.HasToken)
                return true;

            if (_restoreAttempted)
                return false;

            _restoreAttempted = true;
            return await RefreshAsync() is not null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var accessToken = _tokens.AccessToken;

        if (string.IsNullOrEmpty(accessToken))
            return await TryRestoreSessionAsync() ? _tokens.AccessToken : null;

        if (!JwtParser.IsExpired(accessToken, TimeSpan.FromMinutes(1)))
            return accessToken;

        await _refreshLock.WaitAsync();
        try
        {
            // Re-read after acquiring: whoever held the lock may have already refreshed, in
            // which case rotating again would strand a chain and look like a replay.
            accessToken = _tokens.AccessToken;

            if (string.IsNullOrEmpty(accessToken))
                return null;

            if (!JwtParser.IsExpired(accessToken, TimeSpan.FromMinutes(1)))
                return accessToken;

            // On a transient failure RefreshAsync returns null without clearing the session, and
            // the old token is still good for up to a minute, so hand it back and retry later.
            return await RefreshAsync() ?? (_tokens.HasToken ? accessToken : null);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Exchanges the refresh cookie for a new access token, returning null when the session is
    /// over or the attempt could not be completed. Callers must hold <see cref="_refreshLock"/>.
    /// </summary>
    private async Task<string?> RefreshAsync()
    {
        try
        {
            var response = await SendWithCredentialsAsync<object>(
                "/api/auth/refresh-token", body: null);

            if (!response.IsSuccessStatusCode)
            {
                // Only a 401 means the session itself is dead. A 5xx or a gateway hiccup is
                // transient — tearing down the session over one would sign the user out for no
                // reason, so leave the token in place and let the next call retry.
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("Refresh token rejected; logging out");
                    _tokens.Clear();
                    return null;
                }

                _logger.LogWarning(
                    "Token refresh failed with {StatusCode}; keeping the session",
                    (int)response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AccessTokenDto>();
            if (result is null)
            {
                _logger.LogWarning("Token refresh returned an empty body; keeping the session");
                return null;
            }

            _tokens.Set(result.AccessToken);
            return result.AccessToken;
        }
        catch (Exception ex)
        {
            // Transport failure (offline, DNS, CORS). Same reasoning as a 5xx above.
            _logger.LogWarning(ex, "Token refresh could not reach the server; keeping the session");
            return null;
        }
    }

    /// <summary>
    /// Posts to an auth endpoint with the browser's credentials attached, so the refresh cookie
    /// travels with the request. <c>PostAsJsonAsync</c> cannot express this — the flag lives on
    /// the request message — so these calls are built by hand.
    /// </summary>
    private Task<HttpResponseMessage> SendWithCredentialsAsync<T>(string uri, T? body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return _http.SendAsync(request);
    }

}
