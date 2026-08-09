using Microsoft.Extensions.Caching.Hybrid;

namespace NutriTrack.Shared.Caching;

/// <summary>
/// Cached access to the lookup tables. These are seed data: shared by every user, read by
/// nearly every catalog and nutrition query, and never written by the application, so they
/// are held for a long window rather than re-joined on each request.
/// </summary>
public class ReferenceDataCache
{
    private const string Key = "reference-data";

    // Long, because nothing in the application mutates these tables; a deploy that reseeds
    // them restarts the process anyway, which empties the in-process cache.
    private static readonly HybridCacheEntryOptions Options = new()
    {
        Expiration = TimeSpan.FromHours(12),
        LocalCacheExpiration = TimeSpan.FromHours(12)
    };

    private static readonly string[] Tags = [CacheTags.ReferenceData];

    private readonly HybridCache _cache;
    private readonly NutriTrackDbContext _db;

    public ReferenceDataCache(HybridCache cache, NutriTrackDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public ValueTask<ReferenceData> GetAsync(CancellationToken ct) =>
        _cache.GetOrCreateAsync(Key, this, static (self, ct) => self.LoadAsync(ct), Options, Tags, ct);

    /// <summary>
    /// Drops the cached snapshot and reloads it. Callers use this when a snapshot is missing
    /// an id they need, which can only happen if the tables were seeded after it was written.
    /// </summary>
    public async ValueTask<ReferenceData> RefreshAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(Key, ct);
        return await GetAsync(ct);
    }

    private async ValueTask<ReferenceData> LoadAsync(CancellationToken ct)
    {
        var nutrients = await _db.Nutrients
            .Select(n => new { n.NutrientId, n.Name, n.Abv, n.MeasurementUnit })
            .ToListAsync(ct);

        var servingUnits = await _db.ServingUnits
            .Select(s => new { s.ServingUnitId, s.Name })
            .ToListAsync(ct);

        var brands = await _db.Brands
            .Select(b => new { b.BrandId, b.Name })
            .ToListAsync(ct);

        return new ReferenceData(
            nutrients.ToDictionary(n => n.NutrientId, n => new NutrientInfo(n.Name, n.Abv, n.MeasurementUnit)),
            servingUnits.ToDictionary(s => s.ServingUnitId, s => s.Name),
            brands.ToDictionary(b => b.BrandId, b => b.Name));
    }
}
