namespace NutriTrack.Shared.Models.Foods;

/// <summary>
/// Query parameters for the food catalog search. Bound from the query string rather than a JSON
/// body, so it carries no serialisation attributes.
/// </summary>
public class SearchFoodsRequest
{
    public string? Search { get; set; }

    public int? BrandId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
