using NutriTrack.Shared.Models.Common;
using NutriTrack.Shared.Models.Foods;

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

    public async Task<FoodDto> GetFood(int foodId, CancellationToken ct)
    {
        var food = await _db.Foods
            .Include(f => f.Brand)
            .Include(f => f.FoodNutrients)
                .ThenInclude(fn => fn.Nutrient)
            .Include(f => f.FoodServings)
                .ThenInclude(fs => fs.ServingUnit)
            .FirstOrDefaultAsync(f => f.FoodId == foodId, ct)
            ?? throw new NotFoundException($"Food {foodId} not found.");

        _logger.LogInformation("Handled {Method}", nameof(GetFood));
        return new FoodDto
        {
            FoodId = food.FoodId,
            Name = food.Name,
            BrandName = food.Brand?.Name,
            Description = food.Description,
            Nutrients = food.FoodNutrients.Select(fn => new FoodNutrientDto
            {
                NutrientName = fn.Nutrient.Name,
                Abbreviation = fn.Nutrient.Abv,
                ValuePer100g = fn.ValuePer100g,
                Unit = fn.Nutrient.MeasurementUnit.ToString()
            }).ToList(),
            Servings = food.FoodServings.Select(fs => new FoodServingDto
            {
                FoodServingId = fs.FoodServingId,
                DisplayName = fs.DisplayName,
                GramWeight = fs.GramWeight,
                ServingUnit = fs.ServingUnit.Name
            }).ToList()
        };
    }

    public async Task<PagedResultDto<FoodSummaryDto>> SearchFoods(
        string? search, int? brandId, int page, int pageSize, CancellationToken ct)
    {
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
            .Select(f => new FoodSummaryDto
            {
                FoodId = f.FoodId,
                Name = f.Name,
                BrandName = f.Brand != null ? f.Brand.Name : null,
                Description = f.Description
            })
            .ToListAsync(ct);

        _logger.LogInformation("Handled {Method}", nameof(SearchFoods));
        return new PagedResultDto<FoodSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
