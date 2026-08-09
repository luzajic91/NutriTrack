namespace NutriTrack.Shared.Caching;

/// <summary>
/// Tags attached to cache entries so related entries can be dropped in one call via
/// <c>HybridCache.RemoveByTagAsync</c>, without the caller tracking individual keys.
/// </summary>
public static class CacheTags
{
    /// <summary>The lookup tables: nutrients, serving units and brands.</summary>
    public const string ReferenceData = "reference-data";

    /// <summary>Anything derived from the food catalog: individual foods and browse pages.</summary>
    public const string Foods = "foods";
}
