using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NutriTrack.Shared.Email;

namespace NutriTrack.Tests.Features;

public class RegisterHandlerTests
{
    private const string ValidPassword = AuthTestContext.ValidPassword;

    private static AuthService CreateService(NutriTrackDbContext db) =>
        AuthTestContext.CreateService(db);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewUserId()
    {
        await using var db = TestHelpers.CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = "User" });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.Register(
            new RegisterRequest { Email = "test@test.com", Password = ValidPassword },
            CancellationToken.None);

        result.Should().Be(1);
        db.Users.Should().HaveCount(1);
        db.Users.First().Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        await using var db = TestHelpers.CreateDb();
        db.Users.Add(new User
        {
            UserId = 1,
            Email = "test@test.com",
            PasswordHash = "x",
            RoleId = 1
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = async () => await service.Register(
            new RegisterRequest { Email = "test@test.com", Password = ValidPassword },
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*Email is already in use*");
    }

    [Fact]
    public async Task Handle_MissingUserRole_ThrowsNotFoundException()
    {
        await using var db = TestHelpers.CreateDb();

        var service = CreateService(db);
        var act = async () => await service.Register(
            new RegisterRequest { Email = "test@test.com", Password = ValidPassword },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Default role not found*");
    }

    [Fact]
    public async Task Handle_ValidCommand_EmailsALinkCarryingThePersistedToken()
    {
        await using var db = TestHelpers.CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = "User" });
        await db.SaveChangesAsync();

        var sender = AuthTestContext.WorkingSender(out var sentBodies);
        await AuthTestContext.CreateService(db, sender.Object).Register(
            new RegisterRequest { Email = "test@test.com", Password = ValidPassword },
            CancellationToken.None);

        var stored = db.EmailConfirmationTokens.Single();
        sentBodies.Single().Should()
            .Contain($"https://localhost/confirm-email?token={stored.Token}");
    }

    [Fact]
    public async Task Handle_EmailDeliveryFails_StillSucceedsAndLeavesAUsableToken()
    {
        await using var db = TestHelpers.CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = "User" });
        await db.SaveChangesAsync();

        var service = AuthTestContext.CreateService(
            db, AuthTestContext.UnreachableSmtpSender().Object);
        var act = async () => await service.Register(
            new RegisterRequest { Email = "test@test.com", Password = ValidPassword },
            CancellationToken.None);

        // A mail outage must not surface as a failed registration: the row is already
        // committed, so throwing here would strand an account nobody can confirm.
        var userId = (await act.Should().NotThrowAsync()).Subject;
        userId.Should().Be(1);
        db.EmailConfirmationTokens.Should()
            .ContainSingle(t => t.UserId == userId && t.ConsumedAt == null);
    }

    [Fact]
    public async Task Handle_EmailDeliveryFails_AccountIsRecoverableByResending()
    {
        await using var db = TestHelpers.CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = "User" });
        await db.SaveChangesAsync();

        await AuthTestContext.CreateService(db, AuthTestContext.UnreachableSmtpSender().Object)
            .Register(
                new RegisterRequest { Email = "test@test.com", Password = ValidPassword },
                CancellationToken.None);

        // SMTP comes back, the user asks for a new mail, and confirmation now works.
        var service = AuthTestContext.CreateService(
            db, AuthTestContext.WorkingSender(out _).Object);
        await service.ResendConfirmationEmail(
            new ResendConfirmationRequest { Email = "test@test.com" }, CancellationToken.None);

        var token = db.EmailConfirmationTokens.Single(t => t.ConsumedAt == null).Token;
        await service.ConfirmEmail(
            new ConfirmEmailRequest { Token = token }, CancellationToken.None);

        db.Users.Single().EmailConfirmed.Should().BeTrue();
    }
}

public class ResendConfirmationTests
{
    private static ResendConfirmationRequest Request(string email = "test@test.com") =>
        new() { Email = email };

    [Fact]
    public async Task Resend_UnconfirmedUser_IssuesAFreshTokenAndSendsIt()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db, emailConfirmed: false);

        var sender = AuthTestContext.WorkingSender(out var sentBodies);
        await AuthTestContext.CreateService(db, sender.Object)
            .ResendConfirmationEmail(Request(), CancellationToken.None);

        var token = db.EmailConfirmationTokens.Single();
        token.IsActive.Should().BeTrue();
        sentBodies.Single().Should().Contain(token.Token);
    }

    [Fact]
    public async Task Resend_RetiresPreviouslyIssuedTokens()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db, emailConfirmed: false);
        db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            EmailConfirmationTokenId = 1,
            UserId = 1,
            Token = "OLD",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
        await db.SaveChangesAsync();

        await AuthTestContext.CreateService(db)
            .ResendConfirmationEmail(Request(), CancellationToken.None);

        db.EmailConfirmationTokens.Single(t => t.Token == "OLD").IsActive.Should().BeFalse();
        db.EmailConfirmationTokens.Should().ContainSingle(t => t.ConsumedAt == null);
    }

    [Fact]
    public async Task Resend_UnknownEmail_IsASilentNoOp()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db, emailConfirmed: false);
        var sender = AuthTestContext.WorkingSender(out _);

        var act = async () => await AuthTestContext.CreateService(db, sender.Object)
            .ResendConfirmationEmail(Request("nobody@test.com"), CancellationToken.None);

        // Distinguishable behaviour here would leak which addresses are registered.
        await act.Should().NotThrowAsync();
        sender.Verify(AuthTestContext.AnySend, Times.Never);
        db.EmailConfirmationTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Resend_AlreadyConfirmedUser_IsASilentNoOp()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db, emailConfirmed: true);
        var sender = AuthTestContext.WorkingSender(out _);

        var act = async () => await AuthTestContext.CreateService(db, sender.Object)
            .ResendConfirmationEmail(Request(), CancellationToken.None);

        await act.Should().NotThrowAsync();
        sender.Verify(AuthTestContext.AnySend, Times.Never);
        db.EmailConfirmationTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Resend_InvalidEmail_ThrowsValidationException()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db, emailConfirmed: false);

        var act = async () => await AuthTestContext.CreateService(db)
            .ResendConfirmationEmail(Request("not-an-email"), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Resend_EmailDeliveryFails_SurfacesToTheCaller()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db, emailConfirmed: false);

        var act = async () => await AuthTestContext
            .CreateService(db, AuthTestContext.UnreachableSmtpSender().Object)
            .ResendConfirmationEmail(Request(), CancellationToken.None);

        // Unlike registration there is nothing to salvage here — the caller asked for a
        // mail and needs to know it did not go out so they can try again.
        await act.Should().ThrowAsync<SocketException>();
    }
}

/// <summary>
/// Covers the paths that return a <see cref="Result"/> rather than throwing. Assertions are on
/// <see cref="Error.Code"/> and <see cref="Error.Type"/> — never on message text, which is
/// display copy and free to change.
/// </summary>
public class LoginTests
{
    [Fact]
    public async Task Login_UnknownEmail_ReturnsInvalidCredentials()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        var result = await AuthTestContext.CreateService(db).Login(
            new LoginRequest { Email = "nobody@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.invalid_credentials");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsInvalidCredentials()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        var result = await AuthTestContext.CreateService(db).Login(
            new LoginRequest { Email = "test@test.com", Password = "wrong-password" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.invalid_credentials");
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ReturnTheSameError()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        var service = AuthTestContext.CreateService(db);

        var unknownEmail = await service.Login(
            new LoginRequest { Email = "nobody@test.com", Password = "wrong-password" },
            CancellationToken.None);
        var wrongPassword = await service.Login(
            new LoginRequest { Email = "test@test.com", Password = "wrong-password" },
            CancellationToken.None);

        // Identical responses; a caller must not be able to tell which emails are registered.
        unknownEmail.Error.Should().BeEquivalentTo(wrongPassword.Error);
    }

    [Fact]
    public async Task Login_UnconfirmedEmail_ReturnsEmailNotConfirmed()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db, emailConfirmed: false);

        var result = await AuthTestContext.CreateService(db).Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.email_not_confirmed");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        var result = await AuthTestContext.CreateService(db).Login(
            new LoginRequest { Email = "test@test.com", Password = AuthTestContext.ValidPassword },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        db.RefreshTokens.Should().HaveCount(1);
    }
}

public class RefreshTokenTests
{
    [Fact]
    public async Task Refresh_UnknownToken_ReturnsRefreshTokenInvalid()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "does-not-exist" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.refresh_token_invalid");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsRefreshTokenInvalid()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(
            db, "expired-token", expiresAt: DateTime.UtcNow.AddDays(-1));

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "expired-token" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.refresh_token_invalid");
    }

    [Fact]
    public async Task Refresh_RevokedToken_ReturnsRefreshTokenInvalid()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(
            db, "revoked-token", revokedAt: DateTime.UtcNow.AddMinutes(-5));

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "revoked-token" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.refresh_token_invalid");
    }

    [Fact]
    public async Task Refresh_ActiveToken_RotatesAndRevokesTheOldOne()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(db, "active-token");

        var result = await AuthTestContext.CreateService(db).RefreshToken(
            new RefreshTokenRequest { RefreshToken = "active-token" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var old = db.RefreshTokens.Single(t => t.Token == "active-token");
        old.IsRevoked.Should().BeTrue();
        old.ReplacedByToken.Should().Be(result.Value.RefreshToken);
    }

    [Fact]
    public async Task Revoke_UnknownToken_ReturnsRefreshTokenInvalid()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);

        var result = await AuthTestContext.CreateService(db).RevokeToken(
            new RevokeTokenRequest { RefreshToken = "does-not-exist" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("auth.refresh_token_invalid");
    }

    [Fact]
    public async Task Revoke_ActiveToken_MarksItRevoked()
    {
        await using var db = TestHelpers.CreateDb();
        await AuthTestContext.SeedUserAsync(db);
        await AuthTestContext.SeedRefreshTokenAsync(db, "active-token");

        var result = await AuthTestContext.CreateService(db).RevokeToken(
            new RevokeTokenRequest { RefreshToken = "active-token" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.RefreshTokens.Single().IsRevoked.Should().BeTrue();
    }
}

/// <summary>Shared setup for the auth tests.</summary>
internal static class AuthTestContext
{
    public const string ValidPassword = "password123";

    public static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-signing-key-long-enough-for-hmac-sha256-abcdef",
                ["Jwt:Issuer"] = "NutriTrack.Tests",
                ["Jwt:Audience"] = "NutriTrack.Tests",
                ["App:ClientBaseUrl"] = "https://localhost"
            })
            .Build();

    /// <summary>Matches any send, for Moq setups and verifications alike.</summary>
    public static Expression<Func<IEmailSender, Task>> AnySend =>
        s => s.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>());

    /// <summary>A sender that succeeds, recording the body of every mail it is handed.</summary>
    public static Mock<IEmailSender> WorkingSender(out List<string> sentBodies)
    {
        var bodies = new List<string>();
        sentBodies = bodies;

        var sender = new Mock<IEmailSender>();
        sender.Setup(AnySend)
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => bodies.Add(body))
            .Returns(Task.CompletedTask);
        return sender;
    }

    /// <summary>A sender that fails the way an SMTP host that is not listening does.</summary>
    public static Mock<IEmailSender> UnreachableSmtpSender()
    {
        var sender = new Mock<IEmailSender>();
        sender.Setup(AnySend).ThrowsAsync(new SocketException(10061)); // connection refused
        return sender;
    }

    public static AuthService CreateService(
        NutriTrackDbContext db, IEmailSender? emailSender = null)
    {
        var configuration = CreateConfiguration();
        return new AuthService(
            db,
            new JwtTokenService(configuration),
            emailSender ?? Mock.Of<IEmailSender>(),
            configuration,
            NullLogger<AuthService>.Instance,
            new RegisterValidator(),
            new LoginValidator(),
            new RefreshTokenValidator(),
            new RevokeTokenValidator(),
            new ConfirmEmailValidator(),
            new ResendConfirmationValidator());
    }

    public static async Task<User> SeedUserAsync(
        NutriTrackDbContext db, bool emailConfirmed = true)
    {
        db.Roles.Add(new Role { RoleId = 1, Name = "User" });

        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(ValidPassword),
            RoleId = 1,
            EmailConfirmed = emailConfirmed
        };
        db.Users.Add(user);

        await db.SaveChangesAsync();
        return user;
    }

    public static async Task SeedRefreshTokenAsync(
        NutriTrackDbContext db,
        string token,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null)
    {
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = 1,
            Token = token,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            RevokedAt = revokedAt
        });
        await db.SaveChangesAsync();
    }
}
