using NutriTrack.Core.Features.MealLogging;

namespace NutriTrack.Core.Persistence;

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
                   mu.Name AS Unit,
                   SUM(fn.ValuePer100g * mei.Grams / 100) AS Total
            FROM   MealEntries me
            JOIN   MealEntryItems mei ON mei.MealEntryId = me.MealEntryId
            JOIN   FoodNutrients fn   ON fn.FoodId = mei.FoodId
            JOIN   Nutrients n        ON n.NutrientId = fn.NutrientId
            JOIN   MeasurementUnits mu ON mu.MeasurementUnitId = n.MeasurementUnitId
            WHERE  me.UserId = @UserId
            AND    CAST(me.ConsumedAt AS DATE) = @Date
            GROUP BY n.Name, n.Abv, mu.Name
            """;

        var rows = await _db.QueryAsync<NutrientRow>(sql, new
        {
            UserId = userId,
            Date = date.ToDateTime(TimeOnly.MinValue)
        });

        return rows.Select(r => new NutrientTotalResponse(
            r.Name, r.Abv, Math.Round(r.Total, 2), r.Unit)).ToList();
    }

    private record NutrientRow(string Name, string Abv, string Unit, decimal Total);
}