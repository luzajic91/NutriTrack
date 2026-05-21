namespace NutriTrack.Shared.Features.FoodCatalog;

public class FoodCatalogService
{
    private readonly NutriTrackDbContext _db;
    private readonly ILogger<FoodCatalogService> _logger;

    public FoodCatalogService(NutriTrackDbContext db, ILogger<FoodCatalogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<FoodResponse> GetFood(int foodId, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(GetFood));

        var food = await _db.Foods
            .Include(f => f.Brand)
            .Include(f => f.FoodNutrients)
                .ThenInclude(fn => fn.Nutrient)
            .Include(f => f.FoodServings)
                .ThenInclude(fs => fs.ServingUnit)
            .FirstOrDefaultAsync(f => f.FoodId == foodId, ct)
            ?? throw new NotFoundException($"Food {foodId} not found.");

        _logger.LogInformation("Handled {Method}", nameof(GetFood));
        return new FoodResponse(
            food.FoodId,
            food.Name,
            food.Brand?.Name,
            food.Description,
            food.FoodNutrients.Select(fn => new FoodNutrientResponse(
                fn.Nutrient.Name,
                fn.Nutrient.Abv,
                fn.ValuePer100g,
                fn.Nutrient.MeasurementUnit.ToString())).ToList(),
            food.FoodServings.Select(fs => new FoodServingResponse(
                fs.FoodServingId,
                fs.DisplayName,
                fs.GramWeight,
                fs.ServingUnit.Name)).ToList());
    }

    public async Task<PagedResult<FoodSummaryResponse>> SearchFoods(
        string? search, int? brandId, int page, int pageSize, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Method}", nameof(SearchFoods));

        var query = _db.Foods
            .Include(f => f.Brand)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search));

        if (brandId.HasValue)
            query = query.Where(f => f.BrandId == brandId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FoodSummaryResponse(
                f.FoodId,
                f.Name,
                f.Brand != null ? f.Brand.Name : null,
                f.Description))
            .ToListAsync(ct);

        _logger.LogInformation("Handled {Method}", nameof(SearchFoods));
        return new PagedResult<FoodSummaryResponse>(items, totalCount, page, pageSize);
    }
}
