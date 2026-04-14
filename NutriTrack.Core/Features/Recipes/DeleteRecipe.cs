namespace NutriTrack.Core.Features.Recipes;

public record DeleteRecipeCommand(int RecipeId) : IRequest;

public class DeleteRecipeHandler : IRequestHandler<DeleteRecipeCommand>
{
    private readonly NutriTrackDbContext _db;
    private readonly CurrentUserService _currentUser;

    public DeleteRecipeHandler(NutriTrackDbContext db, CurrentUserService currentUser)
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