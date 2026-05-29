namespace NutriTrack.Shared.Persistence;

public static class NutriTrackDbContextExtensions
{
    /// <summary>
    /// Verifies that every supplied food id exists, throwing
    /// <see cref="NotFoundException"/> for the first id that does not.
    /// Shared by recipe creation and meal logging.
    /// </summary>
    public static async Task EnsureFoodsExistAsync(
        this NutriTrackDbContext db, IEnumerable<int> foodIds, CancellationToken ct)
    {
        var ids = foodIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        var existingIds = await db.Foods
            .Where(f => ids.Contains(f.FoodId))
            .Select(f => f.FoodId)
            .ToListAsync(ct);

        var firstMissingFoodId = ids.FirstOrDefault(id => !existingIds.Contains(id));
        if (firstMissingFoodId != default)
            throw new NotFoundException($"Food {firstMissingFoodId} not found.");
    }
}
