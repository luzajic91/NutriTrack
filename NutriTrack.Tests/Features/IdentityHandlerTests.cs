using Microsoft.EntityFrameworkCore;

namespace NutriTrack.Tests.Features;

public class RegisterHandlerTests
{
    private static NutriTrackDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NutriTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NutriTrackDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewUserId()
    {
        await using var db = CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = "User" });
        await db.SaveChangesAsync();

        var handler = new RegisterHandler(db);
        var result = await handler.Handle(
            new RegisterCommand("test@test.com", "password123"),
            CancellationToken.None);

        result.Should().Be(1);
        db.Users.Should().HaveCount(1);
        db.Users.First().Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            UserId = 1,
            Email = "test@test.com",
            PasswordHash = "x",
            RoleId = 1
        });
        await db.SaveChangesAsync();

        var handler = new RegisterHandler(db);
        var act = async () => await handler.Handle(
            new RegisterCommand("test@test.com", "password123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*Email is already in use*");
    }

    [Fact]
    public async Task Handle_MissingUserRole_ThrowsNotFoundException()
    {
        await using var db = CreateDb();

        var handler = new RegisterHandler(db);
        var act = async () => await handler.Handle(
            new RegisterCommand("test@test.com", "password123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Default role not found*");
    }
}