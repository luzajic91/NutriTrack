using NutriTrack.Shared.Models.Meals;
using NutriTrack.Shared.Models.Recipes;

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

    public async Task<int> CreateRecipe(CreateRecipeRequest cmd, CancellationToken ct)
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

    public async Task<RecipeDto> GetRecipe(int recipeId, CancellationToken ct)
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
        return new RecipeDto
        {
            RecipeId = recipe.RecipeId,
            Name = recipe.Name,
            Description = recipe.Description,
            ServingsCount = recipe.ServingsCount,
            TotalGrams = recipe.TotalGrams,
            IsPublic = recipe.IsPublic,
            Items = recipe.RecipeItems.Select(i => new RecipeItemDto
            {
                RecipeItemId = i.RecipeItemId,
                FoodId = i.FoodId,
                FoodName = foods.GetValueOrDefault(i.FoodId, "Unknown"),
                Grams = i.Grams
            }).ToList()
        };
    }

    public async Task<List<RecipeSummaryDto>> ListMyRecipes(CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(ListMyRecipes));

        var result = await _db.Recipes
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

        _logger.LogInformation("Handled {Method}", nameof(ListMyRecipes));
        return result;
    }

    public async Task<List<RecipeSummaryDto>> ListAvailableRecipes(CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(ListAvailableRecipes));

        var result = await _db.Recipes
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

        _logger.LogInformation("Handled {Method}", nameof(ListAvailableRecipes));
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

    public async Task<RecipeNutritionDto> GetRecipeNutrition(int recipeId, CancellationToken ct)
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

        var contributions = nutrients.Select(fn => new NutrientContribution(
            fn.Nutrient.Name,
            fn.Nutrient.Abv,
            fn.Nutrient.MeasurementUnit,
            fn.ValuePer100g,
            gramsLookup.GetValueOrDefault(fn.FoodId)));

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

        _logger.LogInformation("Handled {Method}", nameof(GetRecipeNutrition));
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
}
