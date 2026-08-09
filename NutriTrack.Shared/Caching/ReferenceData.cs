namespace NutriTrack.Shared.Caching;

/// <summary>
/// A snapshot of the lookup tables, keyed by id. Held as a single cache entry because the
/// three tables are always needed together and total a few dozen rows.
/// </summary>
public record ReferenceData(
    Dictionary<int, NutrientInfo> Nutrients,
    Dictionary<int, string> ServingUnitNames,
    Dictionary<int, string> BrandNames);

/// <summary>The nutrient metadata a food response needs, without the entity graph.</summary>
public record NutrientInfo(string Name, string Abv, MeasurementUnit Unit);
