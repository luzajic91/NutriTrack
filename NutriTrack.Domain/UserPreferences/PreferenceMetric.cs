namespace NutriTrack.Domain.UserPreferences;

/// <summary>
/// Identifies which user-preference value a history entry tracks. Int-backed
/// (stored via HasConversion&lt;int&gt;) so the numeric ids are stable.
/// </summary>
public enum PreferenceMetric
{
    WeightKg = 1,
    CalorieGoal = 2,
    ProteinGoalG = 3,
    CarbGoalG = 4,
    FatGoalG = 5,
    FiberGoalG = 6
}
