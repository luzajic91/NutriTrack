namespace NutriTrack.Core.Features.Recipes;

public record RecipeSummaryResponse(
    int RecipeId,
    string Name,
    string? Description,
    int? ServingsCount,
    decimal TotalGrams,
    bool IsPublic,
    int ItemCount);