using NutriTrack.Shared.Features.UserPreferences;

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
    public async Task<IActionResult> Update(UpdateUserPreferencesCommand cmd, CancellationToken ct)
    {
        await _userPreferences.UpdateAsync(cmd, ct);
        return NoContent();
    }
}
