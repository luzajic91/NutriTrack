using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.Recipes
{
    public record ListMyRecipesQuery : IRequest<List<RecipeSummaryResponse>>;

    public record RecipeSummaryResponse(
        int RecipeId,
        string Name,
        string? Description,
        int? ServingsCount,
        decimal TotalGrams,
        bool IsPublic,
        int ItemCount);

    public class ListMyRecipesHandler : IRequestHandler<ListMyRecipesQuery, List<RecipeSummaryResponse>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public ListMyRecipesHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<List<RecipeSummaryResponse>> Handle(
            ListMyRecipesQuery req, CancellationToken ct)
        {
            return await EntityFrameworkQueryableExtensions.ToListAsync(
                _db.Recipes
            .Where(r => r.UserId == _currentUser.UserId)
            .OrderBy(r => r.Name)
            .Select(r => new RecipeSummaryResponse(
                r.RecipeId,
                r.Name,
                r.Description,
                r.ServingsCount,
                r.TotalGrams,
                r.IsPublic,
                r.RecipeItems.Count)),
            ct);
            /*return await _db.Recipes
                .Where(r => r.UserId == _currentUser.UserId)
                .OrderBy(r => r.Name)
                .Select(r => new RecipeSummaryResponse(
                    r.RecipeId,
                    r.Name,
                    r.Description,
                    r.ServingsCount,
                    r.TotalGrams,
                    r.IsPublic,
                    r.RecipeItems.Count))
                .AsQueryable()
                .ToListAsync(ct);*/
        }
    }
}
