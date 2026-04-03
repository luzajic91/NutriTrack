using NutriTrack.Application.Features.MealLogging;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Interfaces
{
    public interface IDailySummaryCacheService
    {
        DailyNutritionSummaryResponse? Get(int userId, DateOnly date);
        void Set(int userId, DateOnly date, DailyNutritionSummaryResponse summary);
        void Invalidate(int userId, DateOnly date);
    }
}
