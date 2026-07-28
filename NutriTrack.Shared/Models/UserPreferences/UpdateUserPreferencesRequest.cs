using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.UserPreferences;

public class UpdateUserPreferencesRequest
{
    [JsonPropertyName("weightKg")]
    public decimal? WeightKg { get; set; }

    [JsonPropertyName("calorieGoal")]
    public int? CalorieGoal { get; set; }

    [JsonPropertyName("proteinGoalG")]
    public int? ProteinGoalG { get; set; }

    [JsonPropertyName("carbGoalG")]
    public int? CarbGoalG { get; set; }

    [JsonPropertyName("fatGoalG")]
    public int? FatGoalG { get; set; }

    [JsonPropertyName("fiberGoalG")]
    public int? FiberGoalG { get; set; }
}
