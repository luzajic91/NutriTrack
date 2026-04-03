using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriTrack.Application.Features.MealLogging;

namespace NutriTrack.Api.Controllers
{
    [ApiController]
    [Route("api/meals")]
    [Authorize]
    public class MealsController : ControllerBase
    {
        private readonly ISender _sender;

        public MealsController(ISender sender) => _sender = sender;

        [HttpPost]
        public async Task<IActionResult> LogMeal(LogMealCommand cmd, CancellationToken ct)
        {
            var id = await _sender.Send(cmd, ct);
            return CreatedAtAction(nameof(LogMeal), new { id }, id);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetMealHistory(
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            CancellationToken ct)
        {
            var result = await _sender.Send(new GetMealHistoryQuery(from, to), ct);
            return Ok(result);
        }

        [HttpGet("daily-summary")]
        public async Task<IActionResult> GetDailyNutritionSummary(
            [FromQuery] DateOnly? date,
            CancellationToken ct)
        {
            var queryDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _sender.Send(new GetDailyNutritionSummaryQuery(queryDate), ct);
            return Ok(result);
        }
    }
}
