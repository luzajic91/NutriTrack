using Microsoft.AspNetCore.RateLimiting;
using NutriTrack.Api.Auth;
using NutriTrack.Api.RateLimiting;

namespace NutriTrack.Api.Controllers;

/// <summary>
/// Every anonymous action here is rate limited per client IP. Adding one without an
/// <see cref="EnableRateLimitingAttribute"/> leaves an unauthenticated endpoint open to
/// abuse, so <c>RateLimitingTests</c> fails the build if one is missing.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly IWebHostEnvironment _environment;

    public AuthController(AuthService auth, IWebHostEnvironment environment)
    {
        _auth = auth;
        _environment = environment;
    }

    // Secure cookies require HTTPS, which the http launch profile does not use. Production is
    // secure by default. Behind a TLS-terminating proxy this needs forwarded headers, or the
    // flag should come from configuration instead.
    private bool CookiesAreSecure => !_environment.IsDevelopment();

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Mail)]
    public async Task<IActionResult> Register(RegisterRequest cmd, CancellationToken ct)
    {
        var userId = await _auth.Register(cmd, ct);
        return CreatedAtAction(nameof(Register), new { userId });
    }

    [HttpPost("confirm-email")]
    [EnableRateLimiting(RateLimitPolicies.Tokens)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest cmd, CancellationToken ct)
    {
        await _auth.ConfirmEmail(cmd, ct);
        return NoContent();
    }

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting(RateLimitPolicies.Mail)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest cmd, CancellationToken ct)
    {
        // Always 202, whether or not that address actually has a pending confirmation:
        // the response must not reveal which emails are registered.
        await _auth.ResendConfirmationEmail(cmd, ct);
        return Accepted();
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Credentials)]
    public async Task<IActionResult> Login(LoginRequest cmd, CancellationToken ct)
    {
        var result = await _auth.Login(cmd, ct);
        return result.ToActionResult(this, IssueTokens);
    }

    /// <summary>
    /// The refresh token arrives in the cookie, never in the body, so no script has to hold it
    /// in order to refresh.
    /// </summary>
    [HttpPost("refresh-token")]
    [EnableRateLimiting(RateLimitPolicies.Tokens)]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var cookieToken = RefreshTokenCookie.Read(Request);

        if (cookieToken is null)
        {
            // No cookie means no session to refresh. Reuses the same error the service returns
            // for a rejected token, so a missing cookie is indistinguishable from an expired
            // one — both to the client, which already handles 401 by logging out, and to anyone
            // probing which sessions exist.
            RefreshTokenCookie.Clear(Response, CookiesAreSecure);
            Result<AuthTokensDto> noSession = AuthErrors.RefreshTokenInvalid;
            return noSession.ToActionResult(this, IssueTokens);
        }

        var result = await _auth.RefreshToken(
            new RefreshTokenRequest { RefreshToken = cookieToken }, ct);

        // A rejected token is dead, and rotation means it will never be valid again, so clear
        // the cookie rather than leaving the browser to keep presenting it.
        if (!result.IsSuccess)
            RefreshTokenCookie.Clear(Response, CookiesAreSecure);

        return result.ToActionResult(this, IssueTokens);
    }

    /// <summary>
    /// Ends the session. Anonymous and cookie-driven on purpose: the access token lives only in
    /// the client's memory, so after a page reload there may be no bearer token to authorise
    /// with while the cookie is still valid. Requiring one would leave the cookie — and the
    /// session behind it — alive. Holding the cookie is itself proof of ownership, and SameSite
    /// stops another site sending it.
    /// </summary>
    [HttpPost("revoke-token")]
    [EnableRateLimiting(RateLimitPolicies.Tokens)]
    public async Task<IActionResult> RevokeToken(CancellationToken ct)
    {
        var cookieToken = RefreshTokenCookie.Read(Request);

        if (cookieToken is not null)
            await _auth.RevokeToken(new RevokeTokenRequest { RefreshToken = cookieToken }, ct);

        // Cleared unconditionally, and the result of the revoke is ignored: logging out must
        // succeed even when the token was already revoked, expired or never existed. Reporting
        // failure here would only tell a caller whether someone else's cookie was still live.
        RefreshTokenCookie.Clear(Response, CookiesAreSecure);
        return NoContent();
    }

    private IActionResult IssueTokens(AuthTokensDto tokens)
    {
        RefreshTokenCookie.Write(
            Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc, CookiesAreSecure);

        return Ok(new AccessTokenDto { AccessToken = tokens.AccessToken });
    }
}
