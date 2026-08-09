namespace NutriTrack.Tests.Features;

/// <summary>
/// Covers replay detection. Refresh tokens live in localStorage, so any XSS can copy one. Rotation
/// means the thief and the real client cannot both keep the chain alive: whoever presents an
/// already-exchanged token reveals that two parties hold it, and the lineage is retired. The
/// important negative case is ordinary expiry, which must not be mistaken for an attack.
/// </summary>
public class RefreshTokenReuseTests
{
    /// <summary>Seeds a rotation chain: each token replaced by the next, only the last active.</summary>
    private static async Task SeedChainAsync(NutriTrackDbContext db, params string[] rawTokens)
    {
        for (var i = 0; i < rawTokens.Length; i++)
        {
            var isLast = i == rawTokens.Length - 1;
            db.RefreshTokens.Add(new RefreshToken
            {
                RefreshTokenId = i + 1,
                UserId = 1,
                Token = TokenHasher.Hash(rawTokens[i]),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10 + i),
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                RevokedAt = isLast ? null : DateTime.UtcNow.AddMinutes(-9 + i),
                ReplacedByToken = isLast ? null : TokenHasher.Hash(rawTokens[i + 1])
            });
        }
        await db.SaveChangesAsync();
    }

    private static RefreshToken Row(NutriTrackDbContext db, string rawToken) =>
        db.RefreshTokens.Single(t => t.Token == TokenHasher.Hash(rawToken));

    [Fact]
    public async Task ReplayingARotatedToken_RevokesTheTokenThatReplacedIt()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await SeedChainAsync(db, "first", "second");

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "first" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        Row(db, "second").IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task ReplayingARotatedToken_RevokesTheWholeDescendantChain()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await SeedChainAsync(db, "first", "second", "third", "fourth");

        await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "first" }, CancellationToken.None);

        // Stopping after one hop would leave the newest token — the one actually in use —
        // working, which is the token the thief is racing the user for.
        db.RefreshTokens.Should().OnlyContain(t => t.RevokedAt != null);
    }

    [Fact]
    public async Task ReplayingARotatedToken_StillReportsTheSameErrorToTheClient()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await SeedChainAsync(db, "first", "second");

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "first" }, CancellationToken.None);

        // The client's handling of a dead refresh token is unchanged: detection is a server-side
        // concern and must not leak into the response.
        result.Error!.Code.Should().Be("auth.refresh_token_invalid");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task AnExpiredToken_RevokesNothing()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(
            db, "expired-token", expiresAt: DateTime.UtcNow.AddDays(-1));
        await AuthTestContext.SeedRefreshTokenAsync(db, "still-good");

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "expired-token" }, CancellationToken.None);

        // Expiry is routine, not evidence of theft. Treating it as a replay would sign people
        // out of every session simply for leaving a tab open too long.
        result.IsSuccess.Should().BeFalse();
        Row(db, "still-good").IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task AReplay_LeavesOtherUsersSessionsAlone()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await SeedChainAsync(db, "first", "second");

        db.Users.Add(new User
        {
            UserId = 2,
            Email = "other@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AuthTestContext.ValidPassword),
            RoleId = 1,
            EmailConfirmed = true
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            RefreshTokenId = 99,
            UserId = 2,
            Token = TokenHasher.Hash("other-users-token"),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "first" }, CancellationToken.None);

        Row(db, "other-users-token").IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task ARevokedTokenWithNoSuccessor_IsRejectedWithoutError()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(
            db, "logged-out", revokedAt: DateTime.UtcNow.AddMinutes(-5));

        // A token revoked by logout has no ReplacedByToken, so there is no chain to walk.
        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "logged-out" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.refresh_token_invalid");
    }

    [Fact]
    public async Task ACyclicChain_DoesNotSpin()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        // Corrupt data: each token claims to be replaced by the other. The walk must terminate.
        db.RefreshTokens.Add(new RefreshToken
        {
            RefreshTokenId = 1,
            UserId = 1,
            Token = TokenHasher.Hash("a"),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow,
            ReplacedByToken = TokenHasher.Hash("b")
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            RefreshTokenId = 2,
            UserId = 1,
            Token = TokenHasher.Hash("b"),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            ReplacedByToken = TokenHasher.Hash("a")
        });
        await db.SaveChangesAsync();

        var act = async () => await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "a" }, CancellationToken.None);

        await act.Should().NotThrowAsync();
        Row(db, "b").IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task TheNormalRotatePath_StillSucceeds()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(db, "active-token");

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "active-token" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        Row(db, "active-token").ReplacedByToken.Should()
            .Be(TokenHasher.Hash(result.Value.RefreshToken));
    }

    [Fact]
    public async Task IssuedRefreshTokens_ExpireInOneDayNotSeven()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        await AuthTestContext.CreateService(db).Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        // Shorter window because the token sits in localStorage where an XSS can read it.
        db.RefreshTokens.Single().ExpiresAt.Should()
            .BeCloseTo(DateTime.UtcNow.AddDays(1), TimeSpan.FromMinutes(1));
    }
}
