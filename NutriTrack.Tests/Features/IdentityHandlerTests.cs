using Microsoft.Extensions.Logging.Abstractions;

namespace NutriTrack.Tests.Features;

public class RegisterHandlerTests
{
    private static AuthService CreateService(NutriTrackDbContext db) =>
        new(db, null!, NullLogger<AuthService>.Instance,
            new RegisterValidator(), new LoginValidator(),
            new RefreshTokenValidator(), new RevokeTokenValidator());

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewUserId()
    {
        await using var db = TestHelpers.CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = "User" });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.Register(
            new RegisterRequest { Email = "test@test.com", Password = "password123" },
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
            new RegisterRequest { Email = "test@test.com", Password = "password123" },
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
            new RegisterRequest { Email = "test@test.com", Password = "password123" },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Default role not found*");
    }
}
