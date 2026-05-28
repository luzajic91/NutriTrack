using NutriTrack.Shared.Models.Meals;
using NutriTrack.Shared.Services;
using System.Net.Http.Json;

namespace NutriTrack.Web.Services;

/// <summary>
/// Handles all meal logging API operations.
/// </summary>
public class MealService : IMealService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public MealService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    public async Task<int> LogMealAsync(LogMealRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await _http.PostAsJsonAsync("/api/meals", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to log meal: {error}");
        }

        var id = await response.Content.ReadFromJsonAsync<int>();
        return id;
    }

    public async Task<List<MealEntryDto>> GetMealHistoryAsync(DateOnly? from = null, DateOnly? to = null)
    {
        await EnsureAuthenticatedAsync();

        var query = "/api/meals/history";
        var hasParam = false;

        if (from.HasValue)
        {
            query += $"?from={from.Value:yyyy-MM-dd}";
            hasParam = true;
        }

        if (to.HasValue)
        {
            query += hasParam ? $"&to={to.Value:yyyy-MM-dd}" : $"?to={to.Value:yyyy-MM-dd}";
        }

        var response = await _http.GetAsync(query);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to load meal history: {error}");
        }

        return await response.Content.ReadFromJsonAsync<List<MealEntryDto>>() ?? new List<MealEntryDto>();
    }

    public async Task<DailyNutritionSummaryDto> GetDailySummaryAsync(DateOnly? date = null)
    {
        await EnsureAuthenticatedAsync();

        var query = date.HasValue
            ? $"/api/meals/daily-summary?date={date.Value:yyyy-MM-dd}"
            : "/api/meals/daily-summary";

        var response = await _http.GetAsync(query);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to load daily summary: {error}");
        }

        return await response.Content.ReadFromJsonAsync<DailyNutritionSummaryDto>()
            ?? new DailyNutritionSummaryDto();
    }

    public async Task<DailyNutritionSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to)
    {
        await EnsureAuthenticatedAsync();

        var query = $"/api/meals/summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        var response = await _http.GetAsync(query);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to load nutrition summary: {error}");
        }

        return await response.Content.ReadFromJsonAsync<DailyNutritionSummaryDto>()
            ?? new DailyNutritionSummaryDto();
    }

    private async Task EnsureAuthenticatedAsync()
    {
        var token = await _authService.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(token))
            throw new Exception("Not authenticated");

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
