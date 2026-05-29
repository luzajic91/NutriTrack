using NutriTrack.Shared.Models.UserPreferences;

namespace NutriTrack.Shared.Services;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto?> GetAsync();
    Task UpdateAsync(UpdateUserPreferencesRequest request);
}
