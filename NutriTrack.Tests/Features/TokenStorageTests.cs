namespace NutriTrack.Tests.Features;

/// <summary>
/// Covers the guarantee that reading the token tables yields nothing usable. Refresh tokens are
/// 7-day bearer credentials, so a plaintext column turns any database read — a leaked backup, an
/// injection, a support query — into working access to every logged-in account.
/// </summary>
public class TokenStorageTests
{
    [Fact]
    public async Task Login_StoresOnlyAHashOfTheRefreshToken()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        var result = await AuthTestContext.CreateService(db).Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        var stored = db.RefreshTokens.Single();
        stored.Token.Should().NotBe(result.Value.RefreshToken);
        stored.Token.Should().Be(TokenHasher.Hash(result.Value.RefreshToken));
    }

    [Fact]
    public async Task IssuedRefreshToken_StillWorksEndToEnd()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        var service = AuthTestContext.CreateService(db);

        var login = await service.Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        // Hashing is only correct if the value handed to the client still authenticates.
        var refreshed = await service.RefreshToken(
            new RefreshTokenRequest { RefreshToken = login.Value.RefreshToken },
            CancellationToken.None);
        refreshed.IsSuccess.Should().BeTrue();

        var revoked = await service.RevokeToken(
            new RevokeTokenRequest { RefreshToken = refreshed.Value.RefreshToken },
            CancellationToken.None);
        revoked.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StoredHashPresentedAsARefreshToken_IsRejected()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        var service = AuthTestContext.CreateService(db);

        await service.Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        // This is the whole point: someone who reads the column and replays what they find
        // gets nowhere, because it hashes again to something that matches no row.
        var result = await service.RefreshToken(
            new RefreshTokenRequest { RefreshToken = db.RefreshTokens.Single().Token },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.refresh_token_invalid");
    }

    [Fact]
    public async Task StoredHashPresentedAsAConfirmationToken_IsRejected()
    {
        await using var db = TestHelpers.CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = "User" });
        await db.SaveChangesAsync();
        var service = AuthTestContext.CreateService(
            db, AuthTestContext.WorkingSender(out _).Object);

        await service.Register(
            new RegisterRequest { Email = "test@test.com", Password = "password123" },
            CancellationToken.None);

        var act = async () => await service.ConfirmEmail(
            new ConfirmEmailRequest { Token = db.EmailConfirmationTokens.Single().Token },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        db.Users.Single().EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task RotationChain_IsStoredHashedToo()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(db, "active-token");

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "active-token" }, CancellationToken.None);

        // ReplacedByToken is a second copy of the new token. Leaving it raw would hand back
        // exactly what hashing Token was meant to withhold.
        var replaced = db.RefreshTokens.Single(t => t.RevokedAt != null);
        replaced.ReplacedByToken.Should().NotBe(result.Value.RefreshToken);
        replaced.ReplacedByToken.Should().Be(TokenHasher.Hash(result.Value.RefreshToken));
    }

    [Fact]
    public void Hash_IsStableAndFixedWidth()
    {
        var hash = TokenHasher.Hash("some-token");

        hash.Should().Be(TokenHasher.Hash("some-token"));
        hash.Should().NotBe(TokenHasher.Hash("some-token "));
        // 64 hex chars fits the existing HasMaxLength(128) columns.
        hash.Should().HaveLength(64);
    }
}
