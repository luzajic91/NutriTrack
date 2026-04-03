using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.MealLogging
{
    public record GetMealHistoryQuery(DateOnly? From, DateOnly? To) : IRequest<List<MealEntryResponse>>;

    public record MealEntryResponse(
        int MealEntryId,
        DateTime ConsumedAt,
        List<MealEntryItemResponse> Items);

    public record MealEntryItemResponse(
        int FoodId,
        string FoodName,
        decimal Grams);

    public class GetMealHistoryHandler : IRequestHandler<GetMealHistoryQuery, List<MealEntryResponse>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetMealHistoryHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<List<MealEntryResponse>> Handle(
            GetMealHistoryQuery req, CancellationToken ct)
        {
            var query = _db.MealEntries
                .Where(m => m.UserId == _currentUser.UserId)
                .AsQueryable();

            if (req.From.HasValue)
                query = query.Where(m => m.ConsumedAt >= req.From.Value.ToDateTime(TimeOnly.MinValue));

            if (req.To.HasValue)
                query = query.Where(m => m.ConsumedAt <= req.To.Value.ToDateTime(TimeOnly.MaxValue));

            var entries = await query
                .OrderByDescending(m => m.ConsumedAt)
                .Include(m => m.Items)
                .ToListAsync(ct);

            var foodIds = entries
                .SelectMany(e => e.Items)
                .Select(i => i.FoodId)
                .Distinct()
                .ToList();

            var foods = await _db.Foods
                .Where(f => foodIds.Contains(f.FoodId))
                .ToDictionaryAsync(f => f.FoodId, f => f.Name, ct);

            return entries.Select(e => new MealEntryResponse(
                e.MealEntryId,
                e.ConsumedAt,
                e.Items.Select(i => new MealEntryItemResponse(
                    i.FoodId,
                    foods.GetValueOrDefault(i.FoodId, "Unknown"),
                    i.Grams)).ToList())).ToList();
        }
    }
}
