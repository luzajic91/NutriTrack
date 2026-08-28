using NutriTrack.Shared.Models.Meals;
using NutriTrack.Shared.Models.Recipes;

namespace NutriTrack.Shared.Features.Recipes;

public class RecipeService
{
    private readonly NutriTrackDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly CreateRecipeValidator _createRecipeValidator;
    private readonly UpdateRecipeValidator _updateRecipeValidator;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(
        NutriTrackDbContext db,
        CurrentUserService currentUser,
        CreateRecipeValidator createRecipeValidator,
        UpdateRecipeValidator updateRecipeValidator,
        ILogger<RecipeService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _createRecipeValidator = createRecipeValidator;
        _updateRecipeValidator = updateRecipeValidator;
        _logger = logger;
    }

    public async Task<int> CreateRecipe(CreateRecipeRequest cmd, CancellationToken ct)
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

        _logger.LogInformation(
            "Recipe {RecipeId} created for user {UserId} ({ItemCount} items)",
            recipe.RecipeId, _currentUser.UserId, recipe.RecipeItems.Count);
        return recipe.RecipeId;
    }

    public async Task<RecipeDto> GetRecipe(int recipeId, CancellationToken ct)
    {
        var recipe = await _db.Recipes
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        EnsureCanView(recipe);

        var foodNames = await GetFoodNamesAsync(recipe.RecipeItems.Select(i => i.FoodId), ct);

        return new RecipeDto
        {
            RecipeId = recipe.RecipeId,
            Name = recipe.Name,
            Description = recipe.Description,
            ServingsCount = recipe.ServingsCount,
            TotalGrams = recipe.TotalGrams,
            IsPublic = recipe.IsPublic,
            IsOwner = recipe.UserId == _currentUser.UserId,
            Items = recipe.RecipeItems.Select(i => new RecipeItemDto
            {
                RecipeItemId = i.RecipeItemId,
                FoodId = i.FoodId,
                FoodName = foodNames.GetValueOrDefault(i.FoodId, "Unknown"),
                Grams = i.Grams
            }).ToList()
        };
    }

    public async Task<List<RecipeSummaryDto>> ListMyRecipes(CancellationToken ct)
    {
        return await _db.Recipes
            .Where(r => r.UserId == _currentUser.UserId)
            .OrderBy(r => r.Name)
            .Select(r => new RecipeSummaryDto
            {
                RecipeId = r.RecipeId,
                Name = r.Name,
                Description = r.Description,
                ServingsCount = r.ServingsCount,
                TotalGrams = r.TotalGrams,
                IsPublic = r.IsPublic,
                ItemCount = r.RecipeItems.Count
            })
            .ToListAsync(ct);
    }

    public async Task<List<RecipeSummaryDto>> ListAvailableRecipes(CancellationToken ct)
    {
        return await _db.Recipes
            .Where(r => r.UserId == _currentUser.UserId || r.IsPublic)
            .OrderBy(r => r.Name)
            .Select(r => new RecipeSummaryDto
            {
                RecipeId = r.RecipeId,
                Name = r.Name,
                Description = r.Description,
                ServingsCount = r.ServingsCount,
                TotalGrams = r.TotalGrams,
                IsPublic = r.IsPublic,
                ItemCount = r.RecipeItems.Count
            })
            .ToListAsync(ct);
    }

    /// <summary>
    /// Replaces the whole recipe, ingredient list included. Editing does not disturb meal
    /// history: MealLoggingService copies a recipe's items into MealEntryItem rows at log
    /// time, so past entries are snapshots and only future logging sees the new ingredients.
    /// </summary>
    public async Task UpdateRecipe(int recipeId, UpdateRecipeRequest cmd, CancellationToken ct)
    {
        _updateRecipeValidator.ValidateAndThrow(cmd);

        var recipe = await _db.Recipes
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        // Ahead of the food lookup on purpose: otherwise a non-owner could read
        // "Food N not found" off a rejected edit and probe which ids exist.
        EnsureCanEdit(recipe);

        await _db.EnsureFoodsExistAsync(cmd.Items.Select(i => i.FoodId), ct);

        recipe.Name = cmd.Name;
        recipe.Description = cmd.Description;
        recipe.ServingsCount = cmd.ServingsCount;
        recipe.IsPublic = cmd.IsPublic;

        // Derived from the items, never taken from the caller — the same rule CreateRecipe
        // follows. TotalGrams is the divisor for portion scaling when a meal logs this recipe.
        recipe.TotalGrams = cmd.Items.Sum(i => i.Grams);

        // Full replace. Nothing outside the recipe holds a RecipeItemId, so dropping the rows
        // and rebuilding costs nothing and keeps this symmetric with CreateRecipe.
        var existingItems = recipe.RecipeItems.ToList();
        _db.RemoveRange(existingItems);
        recipe.RecipeItems.Clear();

        foreach (var item in cmd.Items)
            recipe.RecipeItems.Add(new RecipeItem { FoodId = item.FoodId, Grams = item.Grams });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Recipe {RecipeId} updated by user {UserId} ({ItemCount} items)",
            recipe.RecipeId, _currentUser.UserId, recipe.RecipeItems.Count);
    }

    public async Task DeleteRecipe(int recipeId, CancellationToken ct)
    {
        var recipe = await _db.Recipes
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId, ct)
            ?? throw new NotFoundException($"Recipe {recipeId} not found.");

        EnsureCanDelete(recipe);

        _db.Remove(recipe);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Recipe {RecipeId} deleted by user {UserId}", recipeId, _currentUser.UserId);
    }

    public async Task<RecipeNutritionDto> GetRecipeNutrition(int recipeId, CancellationToken ct)
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

        var contributions = nutrients.Select(fn => new NutrientContribution(
            fn.Nutrient.Name,
            fn.Nutrient.Abv,
            fn.Nutrient.MeasurementUnit,
            fn.ValuePer100g,
            gramsByFood.GetValueOrDefault(fn.FoodId)));

        var totals = NutritionAggregator.Aggregate(contributions);

        List<NutrientTotalDto>? perServing = null;
        if (recipe.ServingsCount.HasValue && recipe.ServingsCount > 0)
        {
            perServing = totals.Select(t => new NutrientTotalDto
            {
                Name = t.Name,
                Abbreviation = t.Abbreviation,
                Total = Math.Round(t.Total / recipe.ServingsCount.Value, 2),
                Unit = t.Unit
            }).ToList();
        }

        return new RecipeNutritionDto
        {
            RecipeId = recipe.RecipeId,
            RecipeName = recipe.Name,
            TotalGrams = recipe.TotalGrams,
            ServingsCount = recipe.ServingsCount,
            Nutrients = totals,
            NutrientsPerServing = perServing
        };
    }

    private void EnsureCanView(Recipe recipe)
    {
        if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
            throw new ForbiddenException("You do not have access to this recipe.");
    }

    private void EnsureCanEdit(Recipe recipe)
    {
        if (recipe.UserId != _currentUser.UserId)
            throw new ForbiddenException("You do not have permission to edit this recipe.");
    }

    private void EnsureCanDelete(Recipe recipe)
    {
        if (recipe.UserId != _currentUser.UserId)
            throw new ForbiddenException("You do not have permission to delete this recipe.");
    }

    private async Task<Dictionary<int, string>> GetFoodNamesAsync(
        IEnumerable<int> foodIds, CancellationToken ct)
    {
        var ids = foodIds.Distinct().ToList();
        return await _db.Foods
            .Where(f => ids.Contains(f.FoodId))
            .ToDictionaryAsync(f => f.FoodId, f => f.Name, ct);
    }
}
