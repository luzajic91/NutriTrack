namespace NutriTrack.Web.Components;

/// <summary>
/// Returned by FoodAndRecipeDropdown for each confirmed selection.
/// IsRecipe=false → individual food; IsRecipe=true → recipe to be expanded by the caller.
/// </summary>
public record FoodOrRecipeSelectionItem(
    int Id,
    string Name,
    decimal Grams,
    bool IsRecipe,
    decimal RecipeTotalGrams = 0m);
