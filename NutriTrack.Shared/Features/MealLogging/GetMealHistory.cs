namespace NutriTrack.Shared.Features.MealLogging;

public record MealEntryResponse(
    int MealEntryId,
    DateTime ConsumedAt,
    List<MealEntryItemResponse> Items);

public record MealEntryItemResponse(
    int FoodId,
    string FoodName,
    decimal Grams);
