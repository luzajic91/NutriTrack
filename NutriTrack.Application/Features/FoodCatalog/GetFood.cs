using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
namespace NutriTrack.Application.Features.FoodCatalog
{
    public record GetFoodQuery(int FoodId) : IRequest<FoodResponse>;

    public record FoodResponse(
        int FoodId,
        string Name,
        string? BrandName,
        string? Description,
        List<FoodNutrientResponse> Nutrients,
        List<FoodServingResponse> Servings);

    public record FoodNutrientResponse(
        string NutrientName,
        string Abbreviation,
        decimal ValuePer100g,
        string Unit);

    public record FoodServingResponse(
        int FoodServingId,
        string DisplayName,
        decimal GramWeight,
        string ServingUnit);

    public class GetFoodHandler : IRequestHandler<GetFoodQuery, FoodResponse>
    {
        private readonly IAppDbContext _db; 

        public GetFoodHandler(IAppDbContext db) => _db = db;

        public async Task<FoodResponse> Handle(GetFoodQuery req, CancellationToken ct)
        {
            var food = await _db.Foods
                .Include(f => f.Brand)
                .Include(f => f.FoodNutrients)
                    .ThenInclude(fn => fn.Nutrient)
                        .ThenInclude(n => n.MeasurementUnit)
                .Include(f => f.FoodServings)
                    .ThenInclude(fs => fs.ServingUnit)
                .FirstOrDefaultAsync(f => f.FoodId == req.FoodId, ct)
                ?? throw new NotFoundException($"Food {req.FoodId} not found.");

            return new FoodResponse(
                food.FoodId,
                food.Name,
                food.Brand?.Name,
                food.Description,
                food.FoodNutrients.Select(fn => new FoodNutrientResponse(
                    fn.Nutrient.Name,
                    fn.Nutrient.Abv,
                    fn.ValuePer100g,
                    fn.Nutrient.MeasurementUnit.Name)).ToList(),
                food.FoodServings.Select(fs => new FoodServingResponse(
                    fs.FoodServingId,
                    fs.DisplayName,
                    fs.GramWeight,
                    fs.ServingUnit.Name)).ToList());
        }
    }
}
