namespace NutriTrack.Shared.Features.MealLogging;

public record DailyNutritionSummaryResponse(
    DateOnly Date,
    List<NutrientTotalResponse> Nutrients);

public record NutrientTotalResponse(
    string Name,
    string Abbreviation,
    decimal Total,
    string Unit);
