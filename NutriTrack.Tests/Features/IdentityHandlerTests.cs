using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Tests.Features
{
    public class RegisterHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();
        private readonly RegisterHandler _handler;

        public RegisterHandlerTests()
        {
            _handler = new RegisterHandler(_db.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsNewUserId()
        {
            // Arrange
            var users = new List<User>();
            var roles = new List<Role> { new() { RoleId = 1, Name = "User" } };

            _db.Setup(x => x.Users)
                .Returns(MockDbSet.CreateMockQueryable(users));
            _db.Setup(x => x.Roles)
                .Returns(MockDbSet.CreateMockQueryable(roles));
            _db.Setup(x => x.Add(It.IsAny<User>()))
                .Callback<User>(u => { u.UserId = 1; users.Add(u); });
            _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var cmd = new RegisterCommand("test@test.com", "password123");

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.Should().Be(1);
            users.Should().HaveCount(1);
            users[0].Email.Should().Be("test@test.com");
        }

        [Fact]
        public async Task Handle_DuplicateEmail_ThrowsValidationException()
        {
            // Arrange
            var users = new List<User>
        {
            new() { UserId = 1, Email = "test@test.com" }
        };

            _db.Setup(x => x.Users)
                .Returns(MockDbSet.CreateMockQueryable(users));

            var cmd = new RegisterCommand("test@test.com", "password123");

            // Act
            var act = async () => await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<FluentValidation.ValidationException>()
                .WithMessage("*Email is already in use*");
        }

        [Fact]
        public async Task Handle_MissingUserRole_ThrowsNotFoundException()
        {
            // Arrange
            _db.Setup(x => x.Users)
                .Returns(MockDbSet.CreateMockQueryable(new List<User>()));
            _db.Setup(x => x.Roles)
                .Returns(MockDbSet.CreateMockQueryable(new List<Role>()));

            var cmd = new RegisterCommand("test@test.com", "password123");

            // Act
            var act = async () => await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Default role not found*");
        }
    }
}
