using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.Recipes
{
    public record GetRecipeQuery(int RecipeId) : IRequest<RecipeResponse>;

    public record RecipeResponse(
        int RecipeId,
        string Name,
        string? Description,
        int? ServingsCount,
        decimal TotalGrams,
        bool IsPublic,
        List<RecipeItemResponse> Items);

    public record RecipeItemResponse(
        int RecipeItemId,
        int FoodId,
        string FoodName,
        decimal Grams);

    public class GetRecipeHandler : IRequestHandler<GetRecipeQuery, RecipeResponse>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetRecipeHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<RecipeResponse> Handle(GetRecipeQuery req, CancellationToken ct)
        {
            var recipe = await _db.Recipes
                .Include(r => r.RecipeItems)
                .FirstOrDefaultAsync(r => r.RecipeId == req.RecipeId, ct)
                ?? throw new NotFoundException($"Recipe {req.RecipeId} not found.");

            if (recipe.UserId != _currentUser.UserId && !recipe.IsPublic)
                throw new ForbiddenException("You do not have access to this recipe.");

            var foodIds = recipe.RecipeItems.Select(i => i.FoodId).ToList();
            var foods = await _db.Foods
                .Where(f => foodIds.Contains(f.FoodId))
                .ToDictionaryAsync(f => f.FoodId, f => f.Name, ct);

            return new RecipeResponse(
                recipe.RecipeId,
                recipe.Name,
                recipe.Description,
                recipe.ServingsCount,
                recipe.TotalGrams,
                recipe.IsPublic,
                recipe.RecipeItems.Select(i => new RecipeItemResponse(
                    i.RecipeItemId,
                    i.FoodId,
                    foods.GetValueOrDefault(i.FoodId, "Unknown"),
                    i.Grams)).ToList());
        }
    }
}
