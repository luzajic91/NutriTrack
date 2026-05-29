namespace NutriTrack.Domain.UserPreferences;

public class UserPreferences
{
    public int UserPreferencesId { get; set; }
    public int UserId { get; set; }
    public decimal? WeightKg { get; set; }
    public int? CalorieGoal { get; set; }
    public int? ProteinGoalG { get; set; }
    public int? CarbGoalG { get; set; }
    public int? FatGoalG { get; set; }
}
