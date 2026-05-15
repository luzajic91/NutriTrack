namespace NutriTrack.Core.Features.FoodCatalog;

public record FoodSummaryResponse(
    int FoodId,
    string Name,
    string? BrandName,
    string? Description);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}