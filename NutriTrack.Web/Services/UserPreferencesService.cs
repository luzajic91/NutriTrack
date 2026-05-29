using NutriTrack.Shared.Models.UserPreferences;
using NutriTrack.Shared.Services;

namespace NutriTrack.Web.Services;

/// <summary>User preferences API operations.</summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly IApiClient _api;

    public UserPreferencesService(IApiClient api) => _api = api;

    public Task<UserPreferencesDto> GetCurrentUserPreferencesAsync() =>
        _api.GetAsync<UserPreferencesDto>("/api/user-preferences");

    public Task UpdateCurrentUserPreferencesAsync(UserPreferencesDto request) =>
        _api.PutAsync("/api/user-preferences", request);
}
