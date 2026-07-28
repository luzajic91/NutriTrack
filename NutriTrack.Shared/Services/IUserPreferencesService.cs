using NutriTrack.Domain.UserPreferences;
using NutriTrack.Shared.Models.UserPreferences;

namespace NutriTrack.Shared.Services;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto> GetCurrentUserPreferencesAsync();
    Task UpdateCurrentUserPreferencesAsync(UserPreferencesDto request);
    Task<PreferenceHistoryDto> GetPreferenceHistoryAsync(PreferenceMetric metric, DateOnly? from = null, DateOnly? to = null);
}
