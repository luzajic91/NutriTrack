using NutriTrack.Shared.Models.UserPreferences;

namespace NutriTrack.Shared.Services;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto> GetCurrentUserPreferencesAsync();
    Task UpdateCurrentUserPreferencesAsync(UserPreferencesDto request);
}
