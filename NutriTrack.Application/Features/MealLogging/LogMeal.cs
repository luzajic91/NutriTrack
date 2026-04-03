using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
using NutriTrack.Domain.MealLogging;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.MealLogging
{
    public record LogMealCommand(
        List<MealFoodEntry> Foods,
        List<MealRecipeEntry> Recipes,
        DateTime? ConsumedAt) : IRequest<int>;

    public record MealFoodEntry(int FoodId, decimal Grams);
    public record MealRecipeEntry(int RecipeId, decimal Grams);

    public class LogMealValidator : AbstractValidator<LogMealCommand>
    {
        public LogMealValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Foods.Count > 0 || x.Recipes.Count > 0)
                .WithMessage("A meal must contain at least one food or recipe.");

            RuleForEach(x => x.Foods).ChildRules(f =>
            {
                f.RuleFor(x => x.FoodId).GreaterThan(0);
                f.RuleFor(x => x.Grams).GreaterThan(0);
            });

            RuleForEach(x => x.Recipes).ChildRules(r =>
            {
                r.RuleFor(x => x.RecipeId).GreaterThan(0);
                r.RuleFor(x => x.Grams).GreaterThan(0);
            });
        }
    }

    public class LogMealHandler : IRequestHandler<LogMealCommand, int>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IDailySummaryCacheService _cache;

        public LogMealHandler(
            IAppDbContext db,
            ICurrentUserService currentUser,
            IDailySummaryCacheService cache)
        {
            _db = db;
            _currentUser = currentUser;
            _cache = cache;
        }

        public async Task<int> Handle(LogMealCommand cmd, CancellationToken ct)
        {
            var consumedAt = cmd.ConsumedAt ?? DateTime.UtcNow;

            var entry = new MealEntry
            {
                UserId = _currentUser.UserId,
                ConsumedAt = consumedAt,
                Items = []
            };

            // direct food entries
            foreach (var f in cmd.Foods)
            {
                var exists = await _db.Foods.AnyAsync(x => x.FoodId == f.FoodId, ct);
                if (!exists)
                    throw new NotFoundException($"Food {f.FoodId} not found.");

                entry.Items.Add(new MealEntryItem
                {
                    FoodId = f.FoodId,
                    Grams = f.Grams
                });
            }

            // recipe entries — expand into constituent foods
            foreach (var r in cmd.Recipes)
            {
                var recipe = await _db.Recipes
                    .Include(x => x.RecipeItems)
                    .FirstOrDefaultAsync(x => x.RecipeId == r.RecipeId, ct)
                    ?? throw new NotFoundException($"Recipe {r.RecipeId} not found.");

                if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
                    throw new ForbiddenException("You do not have access to this recipe.");

                var scale = r.Grams / recipe.TotalGrams;

                foreach (var item in recipe.RecipeItems)
                    entry.Items.Add(new MealEntryItem
                    {
                        FoodId = item.FoodId,
                        Grams = Math.Round(item.Grams * scale, 2)
                    });
            }

            _db.Add(entry);
            await _db.SaveChangesAsync(ct);

            // invalidate daily summary cache for this user+date
            _cache.Invalidate(_currentUser.UserId, DateOnly.FromDateTime(consumedAt));

            return entry.MealEntryId;
        }
    }
}
