using NutriTrack.Shared.Models.Recipes;
using NutriTrack.Shared.Services;
using System.Net.Http.Json;

namespace NutriTrack.Web.Services;

/// <summary>
/// Handles all recipe-related API operations.
/// SQL Analogy: Like stored procedures for recipe CRUD operations.
/// </summary>
public class RecipeService : IRecipeService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public RecipeService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    /// <summary>
    /// Get all recipes owned by the current user.
    /// SQL Analogy: SELECT * FROM Recipes WHERE UserId = @CurrentUserId
    /// </summary>
    public async Task<List<RecipeSummaryDto>> GetMyRecipesAsync()
    {
        // Ensure we have a valid access token
        await EnsureAuthenticatedAsync();

        var response = await _http.GetAsync("/api/recipes/my");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to load recipes: {error}");
        }

        var recipes = await response.Content.ReadFromJsonAsync<List<RecipeSummaryDto>>();
        return recipes ?? new List<RecipeSummaryDto>();
    }

    /// <summary>
    /// Get a single recipe by ID.
    /// SQL Analogy: SELECT * FROM Recipes WHERE RecipeId = @Id
    /// </summary>
    public async Task<RecipeDto> GetRecipeAsync(int id)
    {
        await EnsureAuthenticatedAsync();

        var response = await _http.GetAsync($"/api/recipes/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to load recipe: {error}");
        }

        var recipe = await response.Content.ReadFromJsonAsync<RecipeDto>();
        return recipe ?? throw new Exception("Recipe not found");
    }

    /// <summary>
    /// Create a new recipe.
    /// SQL Analogy: INSERT INTO Recipes (...) VALUES (...)
    /// </summary>
    public async Task<int> CreateRecipeAsync(CreateRecipeRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await _http.PostAsJsonAsync("/api/recipes", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create recipe: {error}");
        }

        // API returns the new recipe ID
        var recipeId = await response.Content.ReadFromJsonAsync<int>();
        return recipeId;
    }

    /// <summary>
    /// Delete a recipe.
    /// SQL Analogy: DELETE FROM Recipes WHERE RecipeId = @Id
    /// </summary>
    public async Task DeleteRecipeAsync(int id)
    {
        await EnsureAuthenticatedAsync();

        var response = await _http.DeleteAsync($"/api/recipes/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete recipe: {error}");
        }
    }

    /// <summary>
    /// Ensures the HTTP client has a valid access token.
    /// Automatically refreshes token if expired.
    /// </summary>
    private async Task EnsureAuthenticatedAsync()
    {
        var token = await _authService.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Not authenticated");
        }

        // Set the Authorization header for this request
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}