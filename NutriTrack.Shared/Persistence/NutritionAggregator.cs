using NutriTrack.Shared.Models.Meals;

namespace NutriTrack.Shared.Persistence;

/// <summary>
/// A single source-of-truth for nutrient totals. One row per (food nutrient, grams consumed);
/// totals are summed per nutrient using the canonical formula ValuePer100g * Grams / 100.
/// </summary>
public readonly record struct NutrientContribution(
    string Name,
    string Abbreviation,
    MeasurementUnit Unit,
    decimal ValuePer100g,
    decimal Grams);

public static class NutritionAggregator
{
    public static List<NutrientTotalDto> Aggregate(IEnumerable<NutrientContribution> rows) =>
        rows
            .GroupBy(r => (r.Name, r.Abbreviation, r.Unit))
            .Select(g => new NutrientTotalDto
            {
                Name = g.Key.Name,
                Abbreviation = g.Key.Abbreviation,
                Total = Math.Round(g.Sum(r => r.ValuePer100g * r.Grams / 100), 2),
                Unit = g.Key.Unit.ToString()
            })
            .ToList();
}
