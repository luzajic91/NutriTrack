using NutriTrack.Shared.Models.Foods;

namespace NutriTrack.Shared.Features.FoodCatalog;

public class SearchFoodsValidator : AbstractValidator<SearchFoodsRequest>
{
    /// <summary>Upper bound on rows per request, and on the pages a cache key can describe.</summary>
    public const int MaxPageSize = 100;

    public SearchFoodsValidator()
    {
        // Page 0 used to reach SQL Server as OFFSET -20 and fail the request outright.
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        // Without an upper bound a single request could ask for the entire catalog, and every
        // distinct value became its own cache entry.
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);

        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.BrandId).GreaterThan(0).When(x => x.BrandId.HasValue);
    }
}
