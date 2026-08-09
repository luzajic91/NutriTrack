namespace NutriTrack.Shared.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task LoginAsync(string email, string password);
    Task RegisterAsync(string email, string password);
    Task ConfirmEmailAsync(string token);
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Rebuilds the session from the refresh cookie after a page load, when the in-memory access
    /// token is gone but the cookie may still be valid. Returns whether a session was recovered.
    /// </summary>
    Task<bool> TryRestoreSessionAsync();
}