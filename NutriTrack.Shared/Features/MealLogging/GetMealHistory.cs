namespace NutriTrack.Shared.Features.MealLogging;

public record MealEntryResponse(
    int MealEntryId,
    DateTime ConsumedAt,
    List<MealEntryItemResponse> Items)
{
    public List<NutrientTotalResponse>? Macros { get; init; }
}

public record MealEntryItemResponse(
    int FoodId,
    string FoodName,
    decimal Grams);
