using Microsoft.Extensions.Caching.Hybrid;
using NutriTrack.Shared.Caching;
using NutriTrack.Shared.Models.Common;
using NutriTrack.Shared.Models.Foods;

namespace NutriTrack.Shared.Features.FoodCatalog;

public class FoodCatalogService
{
    // The catalog is seed data shared by every user, so entries are safe to hold and reuse
    // across requests. Shorter than the reference tables only because foods are the thing
    // most likely to be edited first if the catalog ever becomes writable.
    private static readonly HybridCacheEntryOptions CatalogOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(30)
    };

    private static readonly string[] Tags = [CacheTags.Foods];

    private readonly NutriTrackDbContext _db;
    private readonly HybridCache _cache;
    private readonly ReferenceDataCache _reference;
    private readonly ILogger<FoodCatalogService> _logger;

    public FoodCatalogService(
        NutriTrackDbContext db,
        HybridCache cache,
        ReferenceDataCache reference,
        ILogger<FoodCatalogService> logger)
    {
        _db = db;
        _cache = cache;
        _reference = reference;
        _logger = logger;
    }

    public async Task<FoodDto> GetFood(int foodId, CancellationToken ct) =>
        await _cache.GetOrCreateAsync(
            $"food:{foodId}",
            (Service: this, foodId),
            static (s, ct) => s.Service.LoadFoodAsync(s.foodId, ct),
            CatalogOptions,
            Tags,
            ct);

    public async Task<PagedResultDto<FoodSummaryDto>> SearchFoods(
        string? search, int? brandId, int page, int pageSize, CancellationToken ct)
    {
        _logger.LogDebug(
            "Searching foods (search={Search}, brandId={BrandId}, page={Page})",
            search, brandId, page);

        // Free-text queries are deliberately not cached: the key space is unbounded and
        // caller-controlled, so anyone could fill the cache with single-use entries. Browsing,
        // optionally filtered by brand, is the repeated shape that benefits.
        if (!string.IsNullOrWhiteSpace(search))
            return await LoadSearchAsync(search, brandId, page, pageSize, ct);

        return await _cache.GetOrCreateAsync(
            $"foods:browse:{brandId?.ToString() ?? "all"}:{page}:{pageSize}",
            (Service: this, brandId, page, pageSize),
            static (s, ct) => s.Service.LoadSearchAsync(null, s.brandId, s.page, s.pageSize, ct),
            CatalogOptions,
            Tags,
            ct);
    }

    private async ValueTask<FoodDto> LoadFoodAsync(int foodId, CancellationToken ct)
    {
        // Nutrient, serving-unit and brand names come from the cached lookup tables rather
        // than three more Includes, so this reads only the food-specific rows.
        var food = await _db.Foods
            .Include(f => f.FoodNutrients)
            .Include(f => f.FoodServings)
            .FirstOrDefaultAsync(f => f.FoodId == foodId, ct)
            ?? throw new NotFoundException($"Food {foodId} not found.");

        var reference = await ReferenceCovering(
            r => food.FoodNutrients.All(fn => r.Nutrients.ContainsKey(fn.NutrientId))
                && food.FoodServings.All(fs => r.ServingUnitNames.ContainsKey(fs.ServingUnitId))
                && (food.BrandId is null || r.BrandNames.ContainsKey(food.BrandId.Value)),
            ct);

        return new FoodDto
        {
            FoodId = food.FoodId,
            Name = food.Name,
            BrandName = food.BrandId is int brandId ? reference.BrandNames[brandId] : null,
            Description = food.Description,
            Nutrients = food.FoodNutrients.Select(fn =>
            {
                var nutrient = reference.Nutrients[fn.NutrientId];
                return new FoodNutrientDto
                {
                    NutrientName = nutrient.Name,
                    Abbreviation = nutrient.Abv,
                    ValuePer100g = fn.ValuePer100g,
                    Unit = nutrient.Unit.ToString()
                };
            }).ToList(),
            Servings = food.FoodServings.Select(fs => new FoodServingDto
            {
                FoodServingId = fs.FoodServingId,
                DisplayName = fs.DisplayName,
                GramWeight = fs.GramWeight,
                ServingUnit = reference.ServingUnitNames[fs.ServingUnitId]
            }).ToList()
        };
    }

    private async ValueTask<PagedResultDto<FoodSummaryDto>> LoadSearchAsync(
        string? search, int? brandId, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Foods.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search));

        if (brandId.HasValue)
            query = query.Where(f => f.BrandId == brandId);

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new { f.FoodId, f.Name, f.BrandId, f.Description })
            .ToListAsync(ct);

        var reference = await ReferenceCovering(
            r => rows.All(row => row.BrandId is null || r.BrandNames.ContainsKey(row.BrandId.Value)),
            ct);

        return new PagedResultDto<FoodSummaryDto>
        {
            Items = rows.Select(row => new FoodSummaryDto
            {
                FoodId = row.FoodId,
                Name = row.Name,
                BrandName = row.BrandId is int id ? reference.BrandNames[id] : null,
                Description = row.Description
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Returns the cached lookup tables, reloading them once if the cached snapshot predates
    /// rows this response needs. Without this a stale snapshot would silently omit a nutrient
    /// or serving unit; foreign keys guarantee the rows exist, so one reload always suffices.
    /// </summary>
    private async ValueTask<ReferenceData> ReferenceCovering(
        Func<ReferenceData, bool> isComplete, CancellationToken ct)
    {
        var reference = await _reference.GetAsync(ct);
        if (isComplete(reference))
            return reference;

        _logger.LogInformation("Cached reference data was missing rows a food needs; reloading");
        return await _reference.RefreshAsync(ct);
    }
}
