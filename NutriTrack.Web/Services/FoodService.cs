using NutriTrack.Shared.Models.Common;
using NutriTrack.Shared.Models.Foods;
using NutriTrack.Shared.Services;
using System.Net.Http.Json;

namespace NutriTrack.Web.Services;

/// <summary>
/// Handles all food catalog API operations.
/// </summary>
public class FoodService : IFoodService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public FoodService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    public async Task<List<FoodSummaryDto>> SearchFoodsAsync(string? search, int page = 1, int pageSize = 20)
    {
        await EnsureAuthenticatedAsync();

        var query = $"/api/foods?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search)}";
        }

        var response = await _http.GetAsync(query);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to search foods: {error}");
        }

        // API returns PagedResult<FoodSummaryResponse>
        // We need to extract just the Items list
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<FoodSummaryDto>>();
        return result?.Items ?? new List<FoodSummaryDto>();
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