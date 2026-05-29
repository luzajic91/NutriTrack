namespace NutriTrack.Shared.Features.MealLogging;

public class MealLoggingService
{
    private readonly NutriTrackDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly NutritionQueryService _nutritionQuery;
    private readonly LogMealValidator _logMealValidator;
    private readonly ILogger<MealLoggingService> _logger;

    public MealLoggingService(
        NutriTrackDbContext db,
        CurrentUserService currentUser,
        NutritionQueryService nutritionQuery,
        LogMealValidator logMealValidator,
        ILogger<MealLoggingService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _nutritionQuery = nutritionQuery;
        _logMealValidator = logMealValidator;
        _logger = logger;
    }

    public async Task<int> LogMeal(LogMealCommand cmd, CancellationToken ct)
    {
        _logMealValidator.ValidateAndThrow(cmd);

        var entry = new MealEntry
        {
            UserId = _currentUser.UserId,
            ConsumedAt = cmd.ConsumedAt ?? DateTime.UtcNow,
            Items = []
        };

        await AddDirectFoodsAsync(entry, cmd.Foods, ct);
        await AddRecipeFoodsAsync(entry, cmd.Recipes, ct);

        _db.Add(entry);
        await _db.SaveChangesAsync(ct);

        return entry.MealEntryId;
    }

    private async Task AddDirectFoodsAsync(
        MealEntry entry, IReadOnlyList<MealFoodEntry> foods, CancellationToken ct)
    {
        if (foods.Count == 0)
            return;

        await _db.EnsureFoodsExistAsync(foods.Select(f => f.FoodId), ct);

        foreach (var food in foods)
            entry.Items.Add(new MealEntryItem { FoodId = food.FoodId, Grams = food.Grams });
    }

    private async Task AddRecipeFoodsAsync(
        MealEntry entry, IReadOnlyList<MealRecipeEntry> recipes, CancellationToken ct)
    {
        foreach (var recipeEntry in recipes)
        {
            var recipe = await _db.Recipes
                .Include(x => x.RecipeItems)
                .FirstOrDefaultAsync(x => x.RecipeId == recipeEntry.RecipeId, ct)
                ?? throw new NotFoundException($"Recipe {recipeEntry.RecipeId} not found.");

            if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
                throw new ForbiddenException("You do not have access to this recipe.");

            var portionScale = recipeEntry.Grams / recipe.TotalGrams;

            foreach (var item in recipe.RecipeItems)
                entry.Items.Add(new MealEntryItem
                {
                    FoodId = item.FoodId,
                    Grams = Math.Round(item.Grams * portionScale, 2)
                });
        }
    }

    public async Task<List<MealEntryResponse>> GetMealHistory(
        DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var query = _db.MealEntries.Where(m => m.UserId == _currentUser.UserId);

        if (from.HasValue)
            query = query.Where(m => m.ConsumedAt >= from.Value.ToDateTime(TimeOnly.MinValue));

        if (to.HasValue)
            query = query.Where(m => m.ConsumedAt <= to.Value.ToDateTime(TimeOnly.MaxValue));

        var entries = await query
            .OrderByDescending(m => m.ConsumedAt)
            .Include(m => m.Items)
            .ToListAsync(ct);

        var foodNames = await GetFoodNamesAsync(
            entries.SelectMany(e => e.Items).Select(i => i.FoodId), ct);

        var macros = entries.Count > 0
            ? await _nutritionQuery.GetMealMacrosAsync(
                _currentUser.UserId,
                from ?? DateOnly.FromDateTime(entries[^1].ConsumedAt),
                to ?? DateOnly.FromDateTime(entries[0].ConsumedAt))
            : new Dictionary<int, List<NutrientTotalResponse>>();

        return entries.Select(e => new MealEntryResponse(
            e.MealEntryId,
            e.ConsumedAt,
            e.Items.Select(i => new MealEntryItemResponse(
                i.FoodId,
                foodNames.GetValueOrDefault(i.FoodId, "Unknown"),
                i.Grams)).ToList())
        {
            Macros = macros.GetValueOrDefault(e.MealEntryId)
        }).ToList();
    }

    public async Task<DailyNutritionSummaryResponse> GetDailyNutritionSummary(
        DateOnly? date, CancellationToken ct)
    {
        var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var nutrients = await _nutritionQuery.GetDailySummaryAsync(_currentUser.UserId, day);
        return new DailyNutritionSummaryResponse(day, nutrients);
    }

    public async Task<DailyNutritionSummaryResponse> GetSummaryRange(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var nutrients = from == to
            ? await _nutritionQuery.GetDailySummaryAsync(_currentUser.UserId, from)
            : await _nutritionQuery.GetSummaryRangeAsync(_currentUser.UserId, from, to);
        return new DailyNutritionSummaryResponse(from, nutrients);
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
