using Microsoft.Extensions.Caching.Memory;
using NutriTrack.Application.Features.MealLogging;
using NutriTrack.Application.Interfaces;

namespace NutriTrack.Infrastructure
{
    public class DailySummaryCacheService : IDailySummaryCacheService
    {
        private readonly IMemoryCache _cache;

        public DailySummaryCacheService(IMemoryCache cache) => _cache = cache;

        private static string Key(int userId, DateOnly date) =>
            $"daily-summary:{userId}:{date:yyyy-MM-dd}";

        public DailyNutritionSummaryResponse? Get(int userId, DateOnly date) =>
            _cache.TryGetValue(Key(userId, date), out DailyNutritionSummaryResponse? value)
                ? value
                : null;

        public void Set(int userId, DateOnly date, DailyNutritionSummaryResponse summary) =>
            _cache.Set(Key(userId, date), summary, TimeSpan.FromHours(1));

        public void Invalidate(int userId, DateOnly date) =>
            _cache.Remove(Key(userId, date));
    }
}
