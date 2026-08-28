using NutriTrack.Shared.Models.Recipes;

namespace NutriTrack.Shared.Services;

public interface IRecipeService
{
    Task<List<RecipeSummaryDto>> GetMyRecipesAsync();
    Task<List<RecipeSummaryDto>> GetAvailableRecipesAsync();
    Task<RecipeDto> GetRecipeAsync(int id);
    Task<int> CreateRecipeAsync(CreateRecipeRequest request);
    Task UpdateRecipeAsync(int id, UpdateRecipeRequest request);
    Task DeleteRecipeAsync(int id);
}