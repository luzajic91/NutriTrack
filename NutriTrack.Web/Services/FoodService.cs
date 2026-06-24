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

    public async Task<FoodDto> GetFoodAsync(int id)
    {
        await EnsureAuthenticatedAsync();

        var response = await _http.GetAsync($"/api/foods/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to load food: {error}");
        }

        var food = await response.Content.ReadFromJsonAsync<FoodDto>();
        return food ?? throw new Exception("Food not found");
    }

    private async Task EnsureAuthenticatedAsync()
    {
        var token = await _authService.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Not authenticated");
        }

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
