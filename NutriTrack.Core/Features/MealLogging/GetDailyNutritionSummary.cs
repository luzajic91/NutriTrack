namespace NutriTrack.Core.Features.MealLogging;

public record GetDailyNutritionSummaryQuery(DateOnly Date) : IRequest<DailyNutritionSummaryResponse>;

public record DailyNutritionSummaryResponse(
    DateOnly Date,
    List<NutrientTotalResponse> Nutrients);

public record NutrientTotalResponse(
    string Name,
    string Abbreviation,
    decimal Total,
    string Unit);

public class GetDailyNutritionSummaryHandler
    : IRequestHandler<GetDailyNutritionSummaryQuery, DailyNutritionSummaryResponse>
{
    private readonly NutritionQueryService _query;
    private readonly CurrentUserService _currentUser;

    public GetDailyNutritionSummaryHandler(
        NutritionQueryService query,
        CurrentUserService currentUser)
    {
        _query = query;
        _currentUser = currentUser;
    }

    public async Task<DailyNutritionSummaryResponse> Handle(
        GetDailyNutritionSummaryQuery req, CancellationToken ct)
    {
        var nutrients = await _query.GetDailySummaryAsync(_currentUser.UserId, req.Date);
        return new DailyNutritionSummaryResponse(req.Date, nutrients);
    }
}