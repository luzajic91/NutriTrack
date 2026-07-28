using NutriTrack.Shared.Features.UserPreferences;
using NutriTrack.Domain.UserPreferences;

namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/user-preferences")]
[Authorize]
public class UserPreferencesController : ControllerBase
{
    private readonly UserPreferencesService _userPreferences;

    public UserPreferencesController(UserPreferencesService userPreferences)
        => _userPreferences = userPreferences;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _userPreferences.GetAsync(ct);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateUserPreferencesRequest cmd, CancellationToken ct)
    {
        await _userPreferences.UpdateAsync(cmd, ct);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] PreferenceMetric metric,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var result = await _userPreferences.GetHistoryAsync(metric, from, to, ct);
        return Ok(result);
    }
}
