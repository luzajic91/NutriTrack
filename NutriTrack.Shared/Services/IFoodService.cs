using NutriTrack.Shared.Models.Foods;

namespace NutriTrack.Shared.Services;

public interface IFoodService
{
    Task<List<FoodSummaryDto>> SearchFoodsAsync(string? search, int page = 1, int pageSize = 20);
    Task<FoodDto> GetFoodAsync(int id);
}