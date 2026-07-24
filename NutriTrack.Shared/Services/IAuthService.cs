namespace NutriTrack.Shared.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task LoginAsync(string email, string password);
    Task RegisterAsync(string email, string password);
    Task ConfirmEmailAsync(string token);
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
}