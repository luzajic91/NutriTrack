using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.Recipes
{
    public record DeleteRecipeCommand(int RecipeId) : IRequest;

    public class DeleteRecipeHandler : IRequestHandler<DeleteRecipeCommand>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DeleteRecipeHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task Handle(DeleteRecipeCommand cmd, CancellationToken ct)
        {
            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(r => r.RecipeId == cmd.RecipeId, ct)
                ?? throw new NotFoundException($"Recipe {cmd.RecipeId} not found.");

            if (recipe.UserId != _currentUser.UserId)
                throw new ForbiddenException("You do not have permission to delete this recipe.");

            _db.Remove(recipe);
            await _db.SaveChangesAsync(ct);
        }
    }
}
