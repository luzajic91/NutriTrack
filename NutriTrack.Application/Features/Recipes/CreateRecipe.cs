using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
using NutriTrack.Domain.Recipes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.Recipes
{
    public record CreateRecipeCommand(
        string Name,
        string? Description,
        int? ServingsCount,
        bool IsPublic,
        List<RecipeItemRequest> Items) : IRequest<int>;

    public record RecipeItemRequest(int FoodId, decimal Grams);

    public class CreateRecipeValidator : AbstractValidator<CreateRecipeCommand>
    {
        public CreateRecipeValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(200);
            RuleFor(x => x.ServingsCount).GreaterThan(0).When(x => x.ServingsCount.HasValue);
            RuleFor(x => x.Items).NotEmpty().WithMessage("A recipe must have at least one item.");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.FoodId).GreaterThan(0);
                item.RuleFor(x => x.Grams).GreaterThan(0);
            });
        }
    }

    public class CreateRecipeHandler : IRequestHandler<CreateRecipeCommand, int>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public CreateRecipeHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<int> Handle(CreateRecipeCommand cmd, CancellationToken ct)
        {
            var foodIds = cmd.Items.Select(i => i.FoodId).ToList();
            /*var existingFoodIds = await _db.Foods
                .Where(f => foodIds.Contains(f.FoodId))
                .Select(f => f.FoodId)
                .AsQueryable()
                .ToListAsync<int>(ct);*/
            var existingFoodIds = await EntityFrameworkQueryableExtensions.ToListAsync(
                _db.Foods
            .Where(f => foodIds.Contains(f.FoodId))
            .Select(f => f.FoodId),
        ct);

            var missingFoodId = foodIds.FirstOrDefault(id => !existingFoodIds.Contains(id));
            if (missingFoodId != default)
                throw new NotFoundException($"Food {missingFoodId} not found.");

            var totalGrams = cmd.Items.Sum(i => i.Grams);

            var recipe = new Recipe
            {
                UserId = _currentUser.UserId,
                Name = cmd.Name,
                Description = cmd.Description,
                ServingsCount = cmd.ServingsCount,
                IsPublic = cmd.IsPublic,
                TotalGrams = totalGrams,
                RecipeItems = cmd.Items.Select(i => new RecipeItem
                {
                    FoodId = i.FoodId,
                    Grams = i.Grams
                }).ToList()
            };

            _db.Add(recipe);
            await _db.SaveChangesAsync(ct);

            return recipe.RecipeId;
        }
    }
}
