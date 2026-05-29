using NutriTrack.Shared.Models.Common;
using NutriTrack.Shared.Models.Foods;
using NutriTrack.Shared.Services;

namespace NutriTrack.Web.Services;

/// <summary>Food catalog API operations.</summary>
public class FoodService : IFoodService
{
    private readonly IApiClient _api;

    public FoodService(IApiClient api) => _api = api;

    public async Task<List<FoodSummaryDto>> SearchFoodsAsync(string? search, int page = 1, int pageSize = 20)
    {
        var query = $"/api/foods?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        var result = await _api.GetAsync<PagedResultDto<FoodSummaryDto>>(query);
        return result.Items;
    }

    public Task<FoodDto> GetFoodAsync(int id) =>
        _api.GetAsync<FoodDto>($"/api/foods/{id}");
}
