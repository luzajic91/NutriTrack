namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/meals")]
[Authorize]
public class MealsController : ControllerBase
{
    private readonly Dispatcher _dispatcher;

    public MealsController(Dispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost]
    public async Task<IActionResult> LogMeal(LogMealCommand cmd, CancellationToken ct)
    {
        var id = await _dispatcher.Send(cmd, ct);
        return CreatedAtAction(nameof(LogMeal), new { id }, id);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetMealHistory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var result = await _dispatcher.Send(new GetMealHistoryQuery(from, to), ct);
        return Ok(result);
    }

    [HttpGet("daily-summary")]
    public async Task<IActionResult> GetDailyNutritionSummary(
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        var queryDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _dispatcher.Send(
            new GetDailyNutritionSummaryQuery(queryDate), ct);
        return Ok(result);
    }
}