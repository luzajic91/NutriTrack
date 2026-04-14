namespace NutriTrack.Core.Features.Recipes;

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
    private readonly NutriTrackDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ListMyRecipesHandler(NutriTrackDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<RecipeSummaryResponse>> Handle(
        ListMyRecipesQuery req, CancellationToken ct)
    {
        return await _db.Recipes
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
            .ToListAsync(ct);
    }
}