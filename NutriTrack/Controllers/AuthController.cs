namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest cmd, CancellationToken ct)
    {
        var userId = await _auth.Register(cmd, ct);
        return CreatedAtAction(nameof(Register), new { userId });
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest cmd, CancellationToken ct)
    {
        await _auth.ConfirmEmail(cmd, ct);
        return NoContent();
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest cmd, CancellationToken ct)
    {
        // Always 202, whether or not that address actually has a pending confirmation:
        // the response must not reveal which emails are registered.
        await _auth.ResendConfirmationEmail(cmd, ct);
        return Accepted();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest cmd, CancellationToken ct)
    {
        var result = await _auth.Login(cmd, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest cmd, CancellationToken ct)
    {
        var result = await _auth.RefreshToken(cmd, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken(RevokeTokenRequest cmd, CancellationToken ct)
    {
        var result = await _auth.RevokeToken(cmd, ct);
        return result.ToActionResult(this);
    }
}
