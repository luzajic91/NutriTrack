namespace NutriTrack.Shared.Features.UserPreferences;

public record UserPreferencesResponse(
    decimal? WeightKg,
    int? CalorieGoal,
    int? ProteinGoalG,
    int? CarbGoalG,
    int? FatGoalG);
