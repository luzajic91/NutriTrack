namespace NutriTrack.Shared.Common;

/// <summary>
/// The well-known authentication failures, declared once so codes stay consistent
/// between the service, the API and the Blazor client.
/// </summary>
public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new(
        "auth.invalid_credentials",
        "Invalid email or password.",
        ErrorType.Unauthorized);

    public static readonly Error EmailNotConfirmed = new(
        "auth.email_not_confirmed",
        "Please confirm your email before logging in.",
        ErrorType.Forbidden);

    /// <summary>
    /// Covers unknown, expired and already-revoked refresh tokens alike. Merging them is
    /// deliberate: distinct responses would let a caller probe which tokens exist.
    /// </summary>
    public static readonly Error RefreshTokenInvalid = new(
        "auth.refresh_token_invalid",
        "Your session has expired. Please sign in again.",
        ErrorType.Unauthorized);
}
