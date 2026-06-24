using NutriTrack.Domain.FoodCatalog;

namespace NutriTrack.Shared.Common;

/// <summary>
/// Shared constants and helpers for nutrient calculations, so rounding precision
/// and unit formatting are defined once rather than repeated across services.
/// </summary>
public static class Nutrition
{
    /// <summary>Decimal places nutrient totals are rounded to for display.</summary>
    public const int NutrientDecimals = 2;

    /// <summary>Rounds a nutrient total to the standard display precision.</summary>
    public static decimal Round(decimal value) => Math.Round(value, NutrientDecimals);
}

public static class MeasurementUnitExtensions
{
    /// <summary>Human-readable name for a measurement unit (e.g. "Grams").</summary>
    public static string ToDisplayString(this MeasurementUnit unit) => unit.ToString();
}
