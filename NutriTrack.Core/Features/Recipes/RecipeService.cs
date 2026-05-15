namespace NutriTrack.Core.Features.Recipes;

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
        _logger.LogInformation("Handling {Method}", nameof(CreateRecipe));

        var foodIds = cmd.Items.Select(i => i.FoodId).ToList();

        var existingFoodIds = await _db.Foods
            .Where(f => foodIds.Contains(f.FoodId))
            .Select(f => f.FoodId)
            .ToListAsync(ct);

        var missingFoodId = foodIds.FirstOrDefault(id => !existingFoodIds.Contains(id));
        if (missingFoodId != default)
            throw new NotFoundException($"Food {missingFoodId} not found.");

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

        _logger.LogInformation("Handled {Method}", nameof(CreateRecipe));
        return recipe.RecipeId;
    }

    public async Task<RecipeResponse> GetRecipe(int recipeId, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(GetRecipe));

        var recipe = await _db.Recipes
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
            throw new ForbiddenException("You do not have access to this recipe.");

        var foodIds = recipe.RecipeItems.Select(i => i.FoodId).ToList();
        var foods = await _db.Foods
            .Where(f => foodIds.Contains(f.FoodId))
            .ToDictionaryAsync(f => f.FoodId, f => f.Name, ct);

        _logger.LogInformation("Handled {Method}", nameof(GetRecipe));
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
                foods.GetValueOrDefault(i.FoodId, "Unknown"),
                i.Grams)).ToList());
    }

    public async Task<List<RecipeSummaryResponse>> ListMyRecipes(CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(ListMyRecipes));

        var result = await _db.Recipes
            .Where(r => r.UserId == _currentUser.UserId)
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

        _logger.LogInformation("Handled {Method}", nameof(ListMyRecipes));
        return result;
    }

    public async Task DeleteRecipe(int recipeId, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(DeleteRecipe));

        var recipe = await _db.Recipes
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        if (recipe.UserId != _currentUser.UserId)
            throw new ForbiddenException("You do not have permission to delete this recipe.");

        _db.Remove(recipe);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Handled {Method}", nameof(DeleteRecipe));
    }

    public async Task<RecipeNutritionResponse> GetRecipeNutrition(int recipeId, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(GetRecipeNutrition));

        var recipe = await _db.Recipes
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
            throw new ForbiddenException("You do not have access to this recipe.");

        var foodIds = recipe.RecipeItems.Select(i => i.FoodId).ToList();

        var nutrients = await _db.FoodNutrients
            .Include(fn => fn.Nutrient)
            .Where(fn => foodIds.Contains(fn.FoodId))
            .ToListAsync(ct);

        var gramsLookup = recipe.RecipeItems.ToDictionary(i => i.FoodId, i => i.Grams);

        var totals = nutrients
            .GroupBy(fn => fn.Nutrient)
            .Select(g => new RecipeNutrientResponse(
                g.Key.Name,
                g.Key.Abv,
                Math.Round(g.Sum(fn =>
                    fn.ValuePer100g * gramsLookup.GetValueOrDefault(fn.FoodId) / 100), 2),
                g.Key.MeasurementUnit.ToString()))
            .ToList();

        List<RecipeNutrientResponse>? perServing = null;
        if (recipe.ServingsCount.HasValue && recipe.ServingsCount > 0)
        {
            perServing = totals.Select(t => t with
            {
                Total = Math.Round(t.Total / recipe.ServingsCount.Value, 2)
            }).ToList();
        }

        _logger.LogInformation("Handled {Method}", nameof(GetRecipeNutrition));
        return new RecipeNutritionResponse(
            recipe.RecipeId,
            recipe.Name,
            recipe.TotalGrams,
            recipe.ServingsCount,
            totals,
            perServing);
    }
}
