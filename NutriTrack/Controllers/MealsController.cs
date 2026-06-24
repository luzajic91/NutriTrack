namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/meals")]
[Authorize]
public class MealsController : ControllerBase
{
    private readonly MealLoggingService _meals;

    public MealsController(MealLoggingService meals) => _meals = meals;

    [HttpPost]
    public async Task<IActionResult> LogMeal(LogMealRequest cmd, CancellationToken ct)
    {
        var id = await _meals.LogMeal(cmd, ct);
        return CreatedAtAction(nameof(LogMeal), new { id }, id);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetMealHistory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var result = await _meals.GetMealHistory(from, to, ct);
        return Ok(result);
    }

    [HttpGet("daily-summary")]
    public async Task<IActionResult> GetDailyNutritionSummary(
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        var result = await _meals.GetDailyNutritionSummary(date, ct);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryRange(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        var result = await _meals.GetSummaryRange(from, to, ct);
        return Ok(result);
    }

}
