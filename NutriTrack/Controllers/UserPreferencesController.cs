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
        var result = await _userPreferences.GetPreferences(ct);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateUserPreferencesCommand cmd, CancellationToken ct)
    {
        await _userPreferences.UpdatePreferences(cmd, ct);
        return NoContent();
    }
}
