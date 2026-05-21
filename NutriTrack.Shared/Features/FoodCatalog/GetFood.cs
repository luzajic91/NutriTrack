namespace NutriTrack.Shared.Features.FoodCatalog;

public record FoodResponse(
    int FoodId,
    string Name,
    string? BrandName,
    string? Description,
    List<FoodNutrientResponse> Nutrients,
    List<FoodServingResponse> Servings);

public record FoodNutrientResponse(
    string NutrientName,
    string Abbreviation,
    decimal ValuePer100g,
    string Unit);

public record FoodServingResponse(
    int FoodServingId,
    string DisplayName,
    decimal GramWeight,
    string ServingUnit);
