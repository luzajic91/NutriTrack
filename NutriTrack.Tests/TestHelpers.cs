namespace NutriTrack.Tests;

/// <summary>
/// Shared helpers for service-level tests: an isolated in-memory database and a
/// <see cref="CurrentUserService"/> backed by a fake authenticated principal.
/// </summary>
public static class TestHelpers
{
    public static NutriTrackDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NutriTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NutriTrackDbContext(options);
    }

    public static CurrentUserService CreateUser(int userId = 1, string role = "User")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext!.User).Returns(principal);
        return new CurrentUserService(accessor.Object);
    }
}
