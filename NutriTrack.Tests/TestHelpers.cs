using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// A real <see cref="HybridCache"/> backed by its default in-process store. Each call
    /// returns an independent instance so cached entries never leak between tests.
    /// </summary>
    public static HybridCache CreateCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
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
