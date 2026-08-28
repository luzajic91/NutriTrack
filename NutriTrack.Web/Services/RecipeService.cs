using NutriTrack.Shared.Models.Recipes;
using NutriTrack.Shared.Services;

namespace NutriTrack.Web.Services;

/// <summary>Recipe CRUD API operations.</summary>
public class RecipeService : IRecipeService
{
    private readonly IApiClient _api;

    public RecipeService(IApiClient api) => _api = api;

    public Task<List<RecipeSummaryDto>> GetMyRecipesAsync() =>
        _api.GetAsync<List<RecipeSummaryDto>>("/api/recipes/my");

    public Task<List<RecipeSummaryDto>> GetAvailableRecipesAsync() =>
        _api.GetAsync<List<RecipeSummaryDto>>("/api/recipes/available");

    public Task<RecipeDto> GetRecipeAsync(int id) =>
        _api.GetAsync<RecipeDto>($"/api/recipes/{id}");

    public Task<int> CreateRecipeAsync(CreateRecipeRequest request) =>
        _api.PostAsync<int>("/api/recipes", request);

    public Task UpdateRecipeAsync(int id, UpdateRecipeRequest request) =>
        _api.PutAsync($"/api/recipes/{id}", request);

    public Task DeleteRecipeAsync(int id) =>
        _api.DeleteAsync($"/api/recipes/{id}");
}
