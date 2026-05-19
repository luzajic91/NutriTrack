namespace NutriTrack.Shared.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task LoginAsync(string email, string password);
    Task RegisterAsync(string email, string password);
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
}