using NutriTrack.Shared.Models.Meals;

namespace NutriTrack.Shared.Persistence;

public class NutritionQueryService
{
    private readonly NutriTrackDbContext _db;

    public NutritionQueryService(NutriTrackDbContext db) => _db = db;

    public async Task<List<NutrientTotalDto>> GetDailySummaryAsync(int userId, DateOnly date)
    {
        var rows = await LoadContributionsAsync(
            userId, date.ToDateTime(TimeOnly.MinValue), date.ToDateTime(TimeOnly.MaxValue));

        return NutritionAggregator.Aggregate(rows.Select(ToContribution));
    }

    public async Task<List<NutrientTotalDto>> GetSummaryRangeAsync(int userId, DateOnly from, DateOnly to)
    {
        var rows = await LoadContributionsAsync(
            userId, from.ToDateTime(TimeOnly.MinValue), to.ToDateTime(TimeOnly.MaxValue));

        var totals = NutritionAggregator.Aggregate(rows.Select(ToContribution));

        var distinctDays = rows.Select(r => r.ConsumedAt.Date).Distinct().Count();
        if (distinctDays > 1)
            foreach (var t in totals)
                t.Total = Math.Round(t.Total / distinctDays, 2);

        return totals;
    }

    public async Task<Dictionary<int, List<NutrientTotalDto>>> GetMealMacrosAsync(
        int userId, DateOnly from, DateOnly to)
    {
        var rows = await LoadContributionsAsync(
            userId, from.ToDateTime(TimeOnly.MinValue), to.ToDateTime(TimeOnly.MaxValue));

        return rows
            .GroupBy(r => r.MealEntryId)
            .ToDictionary(
                g => g.Key,
                g => NutritionAggregator.Aggregate(g.Select(ToContribution)));
    }

    // Filter on the real ConsumedAt column and project to an anonymous type (both
    // SQL-translatable), then materialize. Mapping/aggregation happen in memory.
    private async Task<List<ContributionRow>> LoadContributionsAsync(
        int userId, DateTime fromDt, DateTime toDt)
    {
        var rows = await (
            from me in _db.MealEntries
            where me.UserId == userId && me.ConsumedAt >= fromDt && me.ConsumedAt <= toDt
            from item in me.Items
            join fn in _db.FoodNutrients on item.FoodId equals fn.FoodId
            select new
            {
                me.MealEntryId,
                me.ConsumedAt,
                fn.Nutrient.Name,
                fn.Nutrient.Abv,
                fn.Nutrient.MeasurementUnit,
                fn.ValuePer100g,
                item.Grams
            }).ToListAsync();

        return rows
            .Select(r => new ContributionRow(
                r.MealEntryId, r.ConsumedAt, r.Name, r.Abv, r.MeasurementUnit, r.ValuePer100g, r.Grams))
            .ToList();
    }

    private static NutrientContribution ToContribution(ContributionRow r) =>
        new(r.Name, r.Abv, r.Unit, r.ValuePer100g, r.Grams);

    private record ContributionRow(
        int MealEntryId,
        DateTime ConsumedAt,
        string Name,
        string Abv,
        MeasurementUnit Unit,
        decimal ValuePer100g,
        decimal Grams);
}
