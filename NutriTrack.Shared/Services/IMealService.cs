using NutriTrack.Shared.Models.Meals;

namespace NutriTrack.Shared.Services;

public interface IMealService
{
    Task<int> LogMealAsync(LogMealRequest request);
    Task<List<MealEntryDto>> GetMealHistoryAsync(DateOnly? from = null, DateOnly? to = null);
    Task<DailyNutritionSummaryDto> GetDailySummaryAsync(DateOnly? date = null);
}
