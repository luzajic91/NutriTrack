using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.Recipes
{
    public record GetRecipeNutritionQuery(int RecipeId) : IRequest<RecipeNutritionResponse>;

    public record RecipeNutritionResponse(
        int RecipeId,
        string RecipeName,
        decimal TotalGrams,
        int? ServingsCount,
        List<NutrientTotalResponse> Nutrients,
        List<NutrientTotalResponse>? NutrientsPerServing);

    public record NutrientTotalResponse(
        string Name,
        string Abbreviation,
        decimal Total,
        string Unit);

    public class GetRecipeNutritionHandler : IRequestHandler<GetRecipeNutritionQuery, RecipeNutritionResponse>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetRecipeNutritionHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<RecipeNutritionResponse> Handle(
            GetRecipeNutritionQuery req, CancellationToken ct)
        {
            var recipe = await _db.Recipes
                .Include(r => r.RecipeItems)
                .FirstOrDefaultAsync(r => r.RecipeId == req.RecipeId, ct)
                ?? throw new NotFoundException($"Recipe {req.RecipeId} not found.");

            if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
                throw new ForbiddenException("You do not have access to this recipe.");

            var foodIds = recipe.RecipeItems.Select(i => i.FoodId).ToList();

            var nutrients = await _db.FoodNutrients
                .Include(fn => fn.Nutrient)
                    .ThenInclude(n => n.MeasurementUnit)
                .Where(fn => foodIds.Contains(fn.FoodId))
                .ToListAsync(ct);

            var gramsLookup = recipe.RecipeItems.ToDictionary(i => i.FoodId, i => i.Grams);

            var totals = nutrients
                .GroupBy(fn => fn.Nutrient)
                .Select(g => new NutrientTotalResponse(
                    g.Key.Name,
                    g.Key.Abv,
                    g.Sum(fn => fn.ValuePer100g * gramsLookup.GetValueOrDefault(fn.FoodId) / 100),
                    g.Key.MeasurementUnit.ToString()))
                .ToList();

            List<NutrientTotalResponse>? perServing = null;
            if (recipe.ServingsCount.HasValue && recipe.ServingsCount > 0)
            {
                perServing = totals.Select(t => t with
                {
                    Total = Math.Round(t.Total / recipe.ServingsCount.Value, 2)
                }).ToList();
            }

            return new RecipeNutritionResponse(
                recipe.RecipeId,
                recipe.Name,
                recipe.TotalGrams,
                recipe.ServingsCount,
                totals,
                perServing);
        }
    }
}
