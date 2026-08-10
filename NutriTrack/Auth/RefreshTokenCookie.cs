namespace NutriTrack.Api.Auth;

/// <summary>
/// Reads and writes the refresh token cookie. This lives in the API project because it is the
/// only layer with an <see cref="HttpContext"/>; <c>NutriTrack.Shared</c> stays transport
/// agnostic, the same split <see cref="ResultExtensions"/> keeps for status codes.
///
/// The token is delivered as a cookie rather than in the response body so that JavaScript
/// cannot read it. An XSS can still act as the user while the page is open, but it can no
/// longer copy out a credential that outlives the page.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "nutritrack_refresh";

    // Scoped to the auth endpoints: nothing else needs the cookie, so nothing else receives it.
    private const string Path = "/api/auth";

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    public static void Write(HttpResponse response, string token, DateTime expiresAtUtc, bool secure) =>
        response.Cookies.Append(Name, token, Options(secure, new DateTimeOffset(expiresAtUtc, TimeSpan.Zero)));

    /// <summary>
    /// Removes the cookie. The options must match those used to write it — a browser matches on
    /// name, path and domain, so a delete with a different path silently leaves the cookie in
    /// place, which is how a logout ends up leaving a usable session behind.
    /// </summary>
    public static void Clear(HttpResponse response, bool secure) =>
        response.Cookies.Delete(Name, Options(secure, expires: null));

    private static CookieOptions Options(bool secure, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = secure,
        // Strict: the browser never attaches this cookie to a request originating from another
        // site, which closes CSRF on the auth endpoints without an antiforgery token. Valid
        // because the client and API are same-site; a cross-site deployment would need None.
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}
