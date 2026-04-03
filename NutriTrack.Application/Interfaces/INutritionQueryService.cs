using NutriTrack.Application.Features.MealLogging;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Interfaces
{
    public interface INutritionQueryService
    {
        Task<DailyNutritionSummaryResponse> GetDailySummaryAsync(int userId, DateOnly date);
    }
}
