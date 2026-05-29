using NutriTrack.Shared.Models.Meals;
using NutriTrack.Shared.Services;

namespace NutriTrack.Web.Services;

/// <summary>Meal logging and nutrition summary API operations.</summary>
public class MealService : IMealService
{
    private readonly IApiClient _api;

    public MealService(IApiClient api) => _api = api;

    public Task<int> LogMealAsync(LogMealRequest request) =>
        _api.PostAsync<int>("/api/meals", request);

    public Task<List<MealEntryDto>> GetMealHistoryAsync(DateOnly? from = null, DateOnly? to = null) =>
        _api.GetAsync<List<MealEntryDto>>(BuildHistoryQuery(from, to));

    public Task<DailyNutritionSummaryDto> GetDailySummaryAsync(DateOnly? date = null) =>
        _api.GetAsync<DailyNutritionSummaryDto>(date.HasValue
            ? $"/api/meals/daily-summary?date={date.Value:yyyy-MM-dd}"
            : "/api/meals/daily-summary");

    public Task<DailyNutritionSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to) =>
        _api.GetAsync<DailyNutritionSummaryDto>(
            $"/api/meals/summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    private static string BuildHistoryQuery(DateOnly? from, DateOnly? to)
    {
        var parameters = new List<string>();
        if (from.HasValue) parameters.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) parameters.Add($"to={to.Value:yyyy-MM-dd}");

        return parameters.Count > 0
            ? $"/api/meals/history?{string.Join("&", parameters)}"
            : "/api/meals/history";
    }
}
