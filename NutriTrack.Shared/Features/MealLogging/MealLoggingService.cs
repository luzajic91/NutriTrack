using NutriTrack.Shared.Models.Meals;

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

    public async Task<int> LogMeal(LogMealRequest cmd, CancellationToken ct)
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

    public async Task<List<MealEntryDto>> GetMealHistory(
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

        var effectiveFrom = from ?? DateOnly.FromDateTime(entries.LastOrDefault()?.ConsumedAt ?? DateTime.UtcNow);
        var effectiveTo   = to   ?? DateOnly.FromDateTime(entries.FirstOrDefault()?.ConsumedAt ?? DateTime.UtcNow);

        var macros = entries.Any()
            ? await _nutritionQuery.GetMealMacrosAsync(_currentUser.UserId, effectiveFrom, effectiveTo)
            : new Dictionary<int, List<NutrientTotalDto>>();

        _logger.LogInformation("Handled {Method}", nameof(GetMealHistory));
        return entries.Select(e => new MealEntryDto
        {
            MealEntryId = e.MealEntryId,
            ConsumedAt = e.ConsumedAt,
            Items = e.Items.Select(i => new MealEntryItemDto
            {
                FoodId = i.FoodId,
                FoodName = foodNames.GetValueOrDefault(i.FoodId, "Unknown"),
                Grams = i.Grams
            }).ToList(),
            Macros = macros.GetValueOrDefault(e.MealEntryId)
        }).ToList();
    }

    public async Task<DailyNutritionSummaryDto> GetDailyNutritionSummary(
        DateOnly date, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(GetDailyNutritionSummary));
        var nutrients = await _nutritionQuery.GetDailySummaryAsync(_currentUser.UserId, date);
        _logger.LogInformation("Handled {Method}", nameof(GetDailyNutritionSummary));
        return new DailyNutritionSummaryDto { Date = date, Nutrients = nutrients };
    }

    public async Task<DailyNutritionSummaryDto> GetSummaryRange(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var nutrients = from == to
            ? await _nutritionQuery.GetDailySummaryAsync(_currentUser.UserId, from)
            : await _nutritionQuery.GetSummaryRangeAsync(_currentUser.UserId, from, to);
        _logger.LogInformation("Handled {Method}", nameof(GetSummaryRange));
        return new DailyNutritionSummaryDto { Date = from, Nutrients = nutrients };
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
