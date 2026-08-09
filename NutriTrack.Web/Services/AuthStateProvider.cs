using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using NutriTrack.Shared.Services;
using System.Security.Claims;

namespace NutriTrack.Web.Services;

/// <summary>
/// Derives authentication state from the in-memory access token. A page load starts with no
/// token, so the first evaluation asks the auth service to rebuild the session from the refresh
/// cookie — without that, every reload would look like a logout.
/// </summary>
public class AuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly TokenStore _tokens;
    private readonly IAuthService _auth;
    private readonly ILogger<AuthStateProvider> _logger;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public AuthStateProvider(
        TokenStore tokens, IAuthService auth, ILogger<AuthStateProvider> logger)
    {
        _tokens = tokens;
        _auth = auth;
        _logger = logger;

        // The service owns the token; this provider only reflects it. Subscribing rather than
        // being called directly is what keeps the two from depending on each other.
        _tokens.Changed += OnTokenChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            if (!_tokens.HasToken)
                await _auth.TryRestoreSessionAsync();

            return BuildState();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve authentication state; treating user as anonymous");
            return new AuthenticationState(_anonymous);
        }
    }

    /// <summary>
    /// Reports the state the store already holds, without trying to restore. The event that
    /// calls this fires while the auth service holds its refresh lock, and that lock is not
    /// reentrant — attempting a restore from here would deadlock the tab.
    /// </summary>
    private void OnTokenChanged() =>
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));

    private AuthenticationState BuildState()
    {
        var token = _tokens.AccessToken;

        if (string.IsNullOrEmpty(token))
            return new AuthenticationState(_anonymous);

        var identity = new ClaimsIdentity(JwtParser.ParseClaims(token), "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void Dispose() => _tokens.Changed -= OnTokenChanged;
}
