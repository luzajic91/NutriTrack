using MediatR;
using NutriTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.MealLogging
{
    public record GetDailyNutritionSummaryQuery(DateOnly Date) : IRequest<DailyNutritionSummaryResponse>;

    public record DailyNutritionSummaryResponse(
        DateOnly Date,
        List<NutrientTotalResponse> Nutrients);

    public record NutrientTotalResponse(
        string Name,
        string Abbreviation,
        decimal Total,
        string Unit);

    public class GetDailyNutritionSummaryHandler
        : IRequestHandler<GetDailyNutritionSummaryQuery, DailyNutritionSummaryResponse>
    {
        private readonly INutritionQueryService _query;
        private readonly IDailySummaryCacheService _cache;
        private readonly ICurrentUserService _currentUser;

        public GetDailyNutritionSummaryHandler(
            INutritionQueryService query,
            IDailySummaryCacheService cache,
            ICurrentUserService currentUser)
        {
            _query = query;
            _cache = cache;
            _currentUser = currentUser;
        }

        public async Task<DailyNutritionSummaryResponse> Handle(
            GetDailyNutritionSummaryQuery req, CancellationToken ct)
        {
            var userId = _currentUser.UserId;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // only cache past days — today is always computed live
            if (req.Date < today)
            {
                var cached = _cache.Get(userId, req.Date);
                if (cached is not null) return cached;
            }

            var result = await _query.GetDailySummaryAsync(userId, req.Date);

            if (req.Date < today)
                _cache.Set(userId, req.Date, result);

            return result;
        }
    }
}
