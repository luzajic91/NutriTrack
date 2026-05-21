namespace NutriTrack.Shared.Features.Recipes;

public record RecipeNutritionResponse(
    int RecipeId,
    string RecipeName,
    decimal TotalGrams,
    int? ServingsCount,
    List<RecipeNutrientResponse> Nutrients,
    List<RecipeNutrientResponse>? NutrientsPerServing);

public record RecipeNutrientResponse(
    string Name,
    string Abbreviation,
    decimal Total,
    string Unit);
