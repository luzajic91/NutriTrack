namespace NutriTrack.Web.Services;

/// <summary>
/// Holds the access token in memory for the lifetime of the page. Nothing is written to
/// localStorage: a token kept only in a field cannot be read back by a later script, and it is
/// gone the moment the tab closes. The refresh token is not here at all — it lives in an
/// HttpOnly cookie the client never sees.
///
/// This also decouples <see cref="AuthService"/> from <see cref="AuthStateProvider"/>. The
/// provider needs to ask the service to restore a session, while the service needs to announce
/// that the token changed; pointing them at each other would be a dependency cycle. They share
/// this instead, and <see cref="Changed"/> carries the announcement.
/// </summary>
public class TokenStore
{
    private string? _accessToken;

    public string? AccessToken => _accessToken;

    public bool HasToken => !string.IsNullOrEmpty(_accessToken);

    /// <summary>Raised whenever the token is set or cleared, so the UI can re-evaluate auth state.</summary>
    public event Action? Changed;

    public void Set(string accessToken)
    {
        _accessToken = accessToken;
        Changed?.Invoke();
    }

    public void Clear()
    {
        _accessToken = null;
        Changed?.Invoke();
    }
}
