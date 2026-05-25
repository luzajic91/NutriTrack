using NutriTrack.Shared.Features.MealLogging;

namespace NutriTrack.Shared.Persistence;

public class NutritionQueryService
{
    private readonly IDbConnection _db;

    public NutritionQueryService(IDbConnection db) => _db = db;

    public async Task<List<NutrientTotalResponse>> GetDailySummaryAsync(
        int userId, DateOnly date)
    {
        const string sql = """
            SELECT n.Name,
                   n.Abv,
                   n.MeasurementUnit,
                   SUM(fn.ValuePer100g * mei.Grams / 100) AS Total
            FROM   MealEntries me
            JOIN   MealEntryItems mei ON mei.MealEntryId = me.MealEntryId
            JOIN   FoodNutrients fn   ON fn.FoodId = mei.FoodId
            JOIN   Nutrients n        ON n.NutrientId = fn.NutrientId
            WHERE  me.UserId = @UserId
            AND    CAST(me.ConsumedAt AS DATE) = @Date
            GROUP BY n.Name, n.Abv, n.MeasurementUnit
            """;

        var rows = await _db.QueryAsync<NutrientRow>(sql, new
        {
            UserId = userId,
            Date = date.ToDateTime(TimeOnly.MinValue)
        });

        return rows.Select(r => new NutrientTotalResponse(
            r.Name, r.Abv, Math.Round(r.Total, 2),
            ((MeasurementUnit)r.MeasurementUnit).ToString())).ToList();
    }

    public async Task<List<NutrientTotalResponse>> GetSummaryRangeAsync(
        int userId, DateOnly from, DateOnly to)
    {
        const string sql = """
            SELECT n.Name,
                   n.Abv,
                   n.MeasurementUnit,
                   SUM(fn.ValuePer100g * mei.Grams / 100)
                       / CAST(COUNT(DISTINCT CAST(me.ConsumedAt AS DATE)) AS DECIMAL) AS Total
            FROM   MealEntries me
            JOIN   MealEntryItems mei ON mei.MealEntryId = me.MealEntryId
            JOIN   FoodNutrients fn   ON fn.FoodId = mei.FoodId
            JOIN   Nutrients n        ON n.NutrientId = fn.NutrientId
            WHERE  me.UserId = @UserId
            AND    CAST(me.ConsumedAt AS DATE) BETWEEN @From AND @To
            GROUP BY n.Name, n.Abv, n.MeasurementUnit
            """;

        var rows = await _db.QueryAsync<NutrientRow>(sql, new
        {
            UserId = userId,
            From = from.ToDateTime(TimeOnly.MinValue),
            To = to.ToDateTime(TimeOnly.MaxValue)
        });

        return rows.Select(r => new NutrientTotalResponse(
            r.Name, r.Abv, Math.Round(r.Total, 2),
            ((MeasurementUnit)r.MeasurementUnit).ToString())).ToList();
    }

    public async Task<List<CalorieTrendPointResponse>> GetCalorieTrendAsync(
        int userId, DateOnly from, DateOnly to)
    {
        const string sql = """
            SELECT CAST(me.ConsumedAt AS DATE)              AS Date,
                   SUM(fn.ValuePer100g * mei.Grams / 100)  AS Calories
            FROM   MealEntries me
            JOIN   MealEntryItems mei ON mei.MealEntryId = me.MealEntryId
            JOIN   FoodNutrients fn   ON fn.FoodId = mei.FoodId
            JOIN   Nutrients n        ON n.NutrientId = fn.NutrientId
            WHERE  me.UserId = @UserId
            AND    n.MeasurementUnit = @KcalUnit
            AND    CAST(me.ConsumedAt AS DATE) BETWEEN @From AND @To
            GROUP BY CAST(me.ConsumedAt AS DATE)
            ORDER BY CAST(me.ConsumedAt AS DATE)
            """;

        var rows = await _db.QueryAsync<TrendRow>(sql, new
        {
            UserId = userId,
            KcalUnit = (int)MeasurementUnit.Kilocalories,
            From = from.ToDateTime(TimeOnly.MinValue),
            To = to.ToDateTime(TimeOnly.MaxValue)
        });

        return rows.Select(r => new CalorieTrendPointResponse(
            DateOnly.FromDateTime(r.Date),
            Math.Round(r.Calories, 0))).ToList();
    }

    public async Task<Dictionary<int, List<NutrientTotalResponse>>> GetMealMacrosAsync(
        int userId, DateOnly from, DateOnly to)
    {
        const string sql = """
            SELECT me.MealEntryId,
                   n.Name,
                   n.Abv,
                   n.MeasurementUnit,
                   SUM(fn.ValuePer100g * mei.Grams / 100) AS Total
            FROM   MealEntries me
            JOIN   MealEntryItems mei ON mei.MealEntryId = me.MealEntryId
            JOIN   FoodNutrients fn   ON fn.FoodId = mei.FoodId
            JOIN   Nutrients n        ON n.NutrientId = fn.NutrientId
            WHERE  me.UserId = @UserId
            AND    CAST(me.ConsumedAt AS DATE) BETWEEN @From AND @To
            GROUP BY me.MealEntryId, n.Name, n.Abv, n.MeasurementUnit
            """;

        var rows = await _db.QueryAsync<MealNutrientRow>(sql, new
        {
            UserId = userId,
            From = from.ToDateTime(TimeOnly.MinValue),
            To = to.ToDateTime(TimeOnly.MaxValue)
        });

        return rows
            .GroupBy(r => r.MealEntryId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new NutrientTotalResponse(
                    r.Name, r.Abv, Math.Round(r.Total, 2),
                    ((MeasurementUnit)r.MeasurementUnit).ToString())).ToList());
    }

    private record NutrientRow(string Name, string Abv, int MeasurementUnit, decimal Total);
    private record TrendRow(DateTime Date, decimal Calories);
    private record MealNutrientRow(int MealEntryId, string Name, string Abv, int MeasurementUnit, decimal Total);
}
