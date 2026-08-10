using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NutriTrack.Api.Auth;
using NutriTrack.Api.Controllers;
using NutriTrack.Shared.Features.Identity;

namespace NutriTrack.Tests.Features;

/// <summary>
/// The refresh token is delivered as an HttpOnly cookie so that no script can read it. These
/// cover the parts that silently stop protecting anything: a flag left off the cookie, the
/// token leaking back into the response body, or a delete whose options do not match the
/// append — which leaves a logged-out session usable.
/// </summary>
public class RefreshTokenCookieTests
{
    private static AuthController CreateController(
        NutriTrackDbContext db, HttpContext? context = null)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Production");

        return new AuthController(AuthTestContext.CreateService(db), environment.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = context ?? new DefaultHttpContext()
            }
        };
    }

    private static HttpContext ContextWithCookie(string? refreshToken)
    {
        var context = new DefaultHttpContext();
        if (refreshToken is not null)
            context.Request.Headers.Cookie = $"{RefreshTokenCookie.Name}={refreshToken}";
        return context;
    }

    private static string SetCookieHeader(HttpContext context) =>
        context.Response.Headers.SetCookie.ToString();

    [Fact]
    public async Task Login_SetsTheRefreshTokenAsAnHttpOnlyCookie()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        var context = new DefaultHttpContext();

        await CreateController(db, context).Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        var setCookie = SetCookieHeader(context);
        setCookie.Should().Contain($"{RefreshTokenCookie.Name}=");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("samesite=strict");
        setCookie.Should().Contain("path=/api/auth");
    }

    [Fact]
    public async Task Login_ResponseBodyCarriesNoRefreshToken()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        var result = await CreateController(db).Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        // The point of the whole change: the client is handed an access token and nothing else.
        var body = result.Should().BeOfType<OkObjectResult>().Subject.Value;
        body.Should().BeOfType<AccessTokenDto>();
        body!.GetType().GetProperty("RefreshToken").Should().BeNull();
    }

    [Fact]
    public async Task Login_CookieOutlivesTheAccessTokenButNotTheRefreshToken()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        var context = new DefaultHttpContext();

        await CreateController(db, context).Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        // An expiry taken from the stored row rather than restated here, so the two cannot drift.
        // Invariant culture because Set-Cookie dates are always English regardless of locale —
        // formatting with the machine's culture passes in en-US and fails everywhere else.
        var stored = db.RefreshTokens.Single().ExpiresAt;
        SetCookieHeader(context).Should().Contain(
            stored.ToString("ddd, dd MMM yyyy HH:mm", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Refresh_WithoutACookie_IsUnauthorizedRatherThanAValidationError()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        var result = await CreateController(db, ContextWithCookie(null))
            .RefreshToken(CancellationToken.None);

        // 401, because to the client a missing cookie is the same event as an expired one and
        // it already knows to log out on 401. A 400 would also reveal that no cookie was sent.
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReadsTheCookieAndRotatesIt()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(db, "active-token");
        var context = ContextWithCookie("active-token");

        var result = await CreateController(db, context).RefreshToken(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();

        // The replacement is issued as a fresh cookie, and it is not the token just used.
        var setCookie = SetCookieHeader(context);
        setCookie.Should().Contain($"{RefreshTokenCookie.Name}=");
        setCookie.Should().NotContain("active-token");
    }

    [Fact]
    public async Task Refresh_WithARejectedToken_ClearsTheCookie()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        var context = ContextWithCookie("never-issued");

        await CreateController(db, context).RefreshToken(CancellationToken.None);

        // Leaving a dead token in the browser means every later request carries a credential
        // that can never work again.
        SetCookieHeader(context).Should().Contain("expires=Thu, 01 Jan 1970");
    }

    [Fact]
    public async Task Revoke_ClearsTheCookieUsingTheSameOptionsItWasWrittenWith()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(db, "active-token");
        var context = ContextWithCookie("active-token");

        var result = await CreateController(db, context).RevokeToken(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();

        // A browser matches a deletion on name and path. A mismatch here leaves the cookie in
        // place and the session alive, which is the classic way logout silently fails.
        var setCookie = SetCookieHeader(context);
        setCookie.Should().Contain("expires=Thu, 01 Jan 1970");
        setCookie.Should().Contain("path=/api/auth");
        setCookie.Should().Contain("samesite=strict");
        db.RefreshTokens.Single().IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Revoke_WithoutACookie_StillSucceedsAndStillClears()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        var context = ContextWithCookie(null);

        var result = await CreateController(db, context).RevokeToken(CancellationToken.None);

        // Logging out must never fail. Reporting an error would also tell a caller whether the
        // cookie they sent belonged to a live session.
        result.Should().BeOfType<NoContentResult>();
        SetCookieHeader(context).Should().Contain("expires=Thu, 01 Jan 1970");
    }

    [Fact]
    public async Task Revoke_WithAnAlreadyRevokedToken_StillSucceeds()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(
            db, "already-gone", revokedAt: DateTime.UtcNow.AddMinutes(-1));

        var result = await CreateController(db, ContextWithCookie("already-gone"))
            .RevokeToken(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void TheCookieIsNotMarkedSecureInDevelopment()
    {
        var response = new DefaultHttpContext().Response;

        // The http launch profile serves plain HTTP, and a Secure cookie would simply be
        // dropped there, breaking local sign-in.
        RefreshTokenCookie.Write(response, "token", DateTime.UtcNow.AddDays(1), secure: false);

        response.Headers.SetCookie.ToString().Should().NotContain("secure");
    }

    [Fact]
    public void TheCookieIsMarkedSecureOutsideDevelopment()
    {
        var response = new DefaultHttpContext().Response;

        RefreshTokenCookie.Write(response, "token", DateTime.UtcNow.AddDays(1), secure: true);

        response.Headers.SetCookie.ToString().Should().Contain("secure");
    }
}
