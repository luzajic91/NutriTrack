namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand cmd, CancellationToken ct)
    {
        var userId = await _auth.Register(cmd, ct);
        return CreatedAtAction(nameof(Register), new { userId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand cmd, CancellationToken ct)
    {
        var result = await _auth.Login(cmd, ct);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var result = await _auth.RefreshToken(cmd, ct);
        return Ok(result);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken(RevokeTokenCommand cmd, CancellationToken ct)
    {
        await _auth.RevokeToken(cmd, ct);
        return NoContent();
    }
}
