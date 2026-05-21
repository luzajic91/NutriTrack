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
        _logger.LogInformation("Handling {Method}", nameof(LogMeal));

        var consumedAt = cmd.ConsumedAt ?? DateTime.UtcNow;

        var entry = new MealEntry
        {
            UserId = _currentUser.UserId,
            ConsumedAt = consumedAt,
            Items = []
        };

        foreach (var f in cmd.Foods)
        {
            var exists = await _db.Foods.AnyAsync(x => x.FoodId == f.FoodId, ct);
            if (!exists)
                throw new NotFoundException($"Food {f.FoodId} not found.");

            entry.Items.Add(new MealEntryItem { FoodId = f.FoodId, Grams = f.Grams });
        }

        foreach (var r in cmd.Recipes)
        {
            var recipe = await _db.Recipes
                .Include(x => x.RecipeItems)
                .FirstOrDefaultAsync(x => x.RecipeId == r.RecipeId, ct)
                ?? throw new NotFoundException($"Recipe {r.RecipeId} not found.");

            if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
                throw new ForbiddenException("You do not have access to this recipe.");

            var scale = r.Grams / recipe.TotalGrams;

            foreach (var item in recipe.RecipeItems)
                entry.Items.Add(new MealEntryItem
                {
                    FoodId = item.FoodId,
                    Grams = Math.Round(item.Grams * scale, 2)
                });
        }

        _db.Add(entry);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Handled {Method}", nameof(LogMeal));
        return entry.MealEntryId;
    }

    public async Task<List<MealEntryResponse>> GetMealHistory(
        DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(GetMealHistory));

        var query = _db.MealEntries
            .Where(m => m.UserId == _currentUser.UserId)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(m =>
                m.ConsumedAt >= from.Value.ToDateTime(TimeOnly.MinValue));

        if (to.HasValue)
            query = query.Where(m =>
                m.ConsumedAt <= to.Value.ToDateTime(TimeOnly.MaxValue));

        var entries = await query
            .OrderByDescending(m => m.ConsumedAt)
            .Include(m => m.Items)
            .ToListAsync(ct);

        var foodIds = entries
            .SelectMany(e => e.Items)
            .Select(i => i.FoodId)
            .Distinct()
            .ToList();

        var foods = await _db.Foods
            .Where(f => foodIds.Contains(f.FoodId))
            .ToDictionaryAsync(f => f.FoodId, f => f.Name, ct);

        _logger.LogInformation("Handled {Method}", nameof(GetMealHistory));
        return entries.Select(e => new MealEntryResponse(
            e.MealEntryId,
            e.ConsumedAt,
            e.Items.Select(i => new MealEntryItemResponse(
                i.FoodId,
                foods.GetValueOrDefault(i.FoodId, "Unknown"),
                i.Grams)).ToList())).ToList();
    }

    public async Task<DailyNutritionSummaryResponse> GetDailyNutritionSummary(
        DateOnly date, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(GetDailyNutritionSummary));
        var nutrients = await _nutritionQuery.GetDailySummaryAsync(_currentUser.UserId, date);
        _logger.LogInformation("Handled {Method}", nameof(GetDailyNutritionSummary));
        return new DailyNutritionSummaryResponse(date, nutrients);
    }
}
