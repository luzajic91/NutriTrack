using NutriTrack.Shared.Models.UserPreferences;
using NutriTrack.Shared.Services;
using System.Net.Http.Json;

namespace NutriTrack.Web.Services;

public class UserPreferencesService : IUserPreferencesService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public UserPreferencesService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    public async Task<UserPreferencesDto?> GetAsync()
    {
        await EnsureAuthenticatedAsync();

        var response = await _http.GetAsync("/api/user-preferences");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to load preferences: {error}");
        }

        return await response.Content.ReadFromJsonAsync<UserPreferencesDto>();
    }

    public async Task UpdateAsync(UpdateUserPreferencesRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await _http.PutAsJsonAsync("/api/user-preferences", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to save preferences: {error}");
        }
    }

    private async Task EnsureAuthenticatedAsync()
    {
        var token = await _authService.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(token))
            throw new Exception("Not authenticated");

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
