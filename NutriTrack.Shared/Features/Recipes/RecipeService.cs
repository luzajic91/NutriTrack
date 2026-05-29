namespace NutriTrack.Shared.Features.Recipes;

public class RecipeService
{
    private readonly NutriTrackDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly CreateRecipeValidator _createRecipeValidator;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(
        NutriTrackDbContext db,
        CurrentUserService currentUser,
        CreateRecipeValidator createRecipeValidator,
        ILogger<RecipeService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _createRecipeValidator = createRecipeValidator;
        _logger = logger;
    }

    public async Task<int> CreateRecipe(CreateRecipeCommand cmd, CancellationToken ct)
    {
        _createRecipeValidator.ValidateAndThrow(cmd);

        await _db.EnsureFoodsExistAsync(cmd.Items.Select(i => i.FoodId), ct);

        var recipe = new Recipe
        {
            UserId = _currentUser.UserId,
            Name = cmd.Name,
            Description = cmd.Description,
            ServingsCount = cmd.ServingsCount,
            IsPublic = cmd.IsPublic,
            TotalGrams = cmd.Items.Sum(i => i.Grams),
            RecipeItems = cmd.Items.Select(i => new RecipeItem
            {
                FoodId = i.FoodId,
                Grams = i.Grams
            }).ToList()
        };

        _db.Add(recipe);
        await _db.SaveChangesAsync(ct);

        return recipe.RecipeId;
    }

    public async Task<RecipeResponse> GetRecipe(int recipeId, CancellationToken ct)
    {
        var recipe = await _db.Recipes
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        EnsureCanView(recipe);

        var foodNames = await GetFoodNamesAsync(recipe.RecipeItems.Select(i => i.FoodId), ct);

        return new RecipeResponse(
            recipe.RecipeId,
            recipe.Name,
            recipe.Description,
            recipe.ServingsCount,
            recipe.TotalGrams,
            recipe.IsPublic,
            recipe.RecipeItems.Select(i => new RecipeItemResponse(
                i.RecipeItemId,
                i.FoodId,
                foodNames.GetValueOrDefault(i.FoodId, "Unknown"),
                i.Grams)).ToList());
    }

    public async Task<List<RecipeSummaryResponse>> ListMyRecipes(CancellationToken ct) =>
        await QuerySummaries(r => r.UserId == _currentUser.UserId, ct);

    public async Task<List<RecipeSummaryResponse>> ListAvailableRecipes(CancellationToken ct) =>
        await QuerySummaries(r => r.UserId == _currentUser.UserId || r.IsPublic, ct);

    public async Task DeleteRecipe(int recipeId, CancellationToken ct)
    {
        var recipe = await _db.Recipes
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        EnsureCanDelete(recipe);

        _db.Remove(recipe);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RecipeNutritionResponse> GetRecipeNutrition(int recipeId, CancellationToken ct)
    {
        var recipe = await _db.Recipes
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        EnsureCanView(recipe);

        var foodIds = recipe.RecipeItems.Select(i => i.FoodId).ToList();

        var nutrients = await _db.FoodNutrients
            .Include(fn => fn.Nutrient)
            .Where(fn => foodIds.Contains(fn.FoodId))
            .ToListAsync(ct);

        var gramsByFood = recipe.RecipeItems.ToDictionary(i => i.FoodId, i => i.Grams);

        var totals = nutrients
            .GroupBy(fn => fn.Nutrient)
            .Select(g => new RecipeNutrientResponse(
                g.Key.Name,
                g.Key.Abv,
                Nutrition.Round(g.Sum(fn =>
                    fn.ValuePer100g * gramsByFood.GetValueOrDefault(fn.FoodId) / 100)),
                g.Key.MeasurementUnit.ToDisplayString()))
            .ToList();

        List<RecipeNutrientResponse>? perServing = null;
        if (recipe.ServingsCount is > 0)
        {
            perServing = totals.Select(t => t with
            {
                Total = Nutrition.Round(t.Total / recipe.ServingsCount.Value)
            }).ToList();
        }

        return new RecipeNutritionResponse(
            recipe.RecipeId,
            recipe.Name,
            recipe.TotalGrams,
            recipe.ServingsCount,
            totals,
            perServing);
    }

    private void EnsureCanView(Recipe recipe)
    {
        if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
            throw new ForbiddenException("You do not have access to this recipe.");
    }

    private void EnsureCanDelete(Recipe recipe)
    {
        if (recipe.UserId != _currentUser.UserId)
            throw new ForbiddenException("You do not have permission to delete this recipe.");
    }

    private async Task<List<RecipeSummaryResponse>> QuerySummaries(
        System.Linq.Expressions.Expression<Func<Recipe, bool>> predicate, CancellationToken ct) =>
        await _db.Recipes
            .Where(predicate)
            .OrderBy(r => r.Name)
            .Select(r => new RecipeSummaryResponse(
                r.RecipeId,
                r.Name,
                r.Description,
                r.ServingsCount,
                r.TotalGrams,
                r.IsPublic,
                r.RecipeItems.Count))
            .ToListAsync(ct);

    private async Task<Dictionary<int, string>> GetFoodNamesAsync(
        IEnumerable<int> foodIds, CancellationToken ct)
    {
        var ids = foodIds.Distinct().ToList();
        return await _db.Foods
            .Where(f => ids.Contains(f.FoodId))
            .ToDictionaryAsync(f => f.FoodId, f => f.Name, ct);
    }
}
