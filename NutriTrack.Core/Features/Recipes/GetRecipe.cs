namespace NutriTrack.Core.Features.Recipes;

public record RecipeResponse(
    int RecipeId,
    string Name,
    string? Description,
    int? ServingsCount,
    decimal TotalGrams,
    bool IsPublic,
    List<RecipeItemResponse> Items);

public record RecipeItemResponse(
    int RecipeItemId,
    int FoodId,
    string FoodName,
    decimal Grams);