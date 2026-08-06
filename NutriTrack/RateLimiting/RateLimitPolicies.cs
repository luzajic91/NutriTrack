namespace NutriTrack.Api.RateLimiting;

/// <summary>
/// Policy names and partition keying for the auth rate limiters. The names are constants
/// because both the registration and the <c>[EnableRateLimiting]</c> attributes reference
/// them — a typo in either would silently leave an endpoint unprotected.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Register and resend-confirmation: one outbound email per request.</summary>
    public const string Mail = "auth-mail";

    /// <summary>Login: brute force and credential stuffing.</summary>
    public const string Credentials = "auth-credentials";

    /// <summary>Confirm-email and refresh-token.</summary>
    public const string Tokens = "auth-tokens";

    /// <summary>Wire contract for a throttled response. Clients match on this.</summary>
    public const string RejectedErrorCode = "error.rate_limited";

    public const string RejectedMessage =
        "Too many requests. Please wait a moment and try again.";

    /// <summary>
    /// Used when the connection has no remote IP. Everything in that state shares a single
    /// bucket, so an unresolvable caller is throttled rather than waved through unlimited.
    /// </summary>
    public const string UnknownClientKey = "unknown";

    /// <summary>
    /// The partition a request counts against. Every limited endpoint is anonymous, so the
    /// client IP is all there is to key on.
    /// </summary>
    /// <remarks>
    /// <see cref="ConnectionInfo.RemoteIpAddress"/> is the true client only on a direct
    /// Kestrel connection. Behind a proxy or load balancer it is the proxy's address, which
    /// collapses every caller into one partition — configure <c>UseForwardedHeaders</c> with
    /// an explicit trusted-proxy list before deploying that way.
    /// </remarks>
    public static string GetClientPartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? UnknownClientKey;
}
