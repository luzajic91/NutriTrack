using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NutriTrack.Shared.Features.UserPreferences;

namespace NutriTrack.Tests.Features;

public class UserPreferencesServiceTests
{
    private static UserPreferencesService CreateService(NutriTrackDbContext db, CurrentUserService user) =>
        new(db, user, new UpdateUserPreferencesValidator(), NullLogger<UserPreferencesService>.Instance);

    [Fact]
    public async Task GetAsync_NoRow_ReturnsNullSafeResponse()
    {
        await using var db = TestHelpers.CreateDb();

        var result = await CreateService(db, TestHelpers.CreateUser()).GetPreferences(CancellationToken.None);

        result.WeightKg.Should().BeNull();
        result.CalorieGoal.Should().BeNull();
        result.ProteinGoalG.Should().BeNull();
        result.CarbGoalG.Should().BeNull();
        result.FatGoalG.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpsertsASingleRow()
    {
        await using var db = TestHelpers.CreateDb();
        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));

        await service.UpdatePreferences(new UpdateUserPreferencesCommand(80, 2000, 150, 200, 65), CancellationToken.None);
        await service.UpdatePreferences(new UpdateUserPreferencesCommand(90, 2100, 160, 210, 70), CancellationToken.None);

        db.UserPreferences.Should().HaveCount(1);
        var saved = await service.GetPreferences(CancellationToken.None);
        saved.WeightKg.Should().Be(90);
        saved.CalorieGoal.Should().Be(2100);
    }

    [Fact]
    public async Task UpdateAsync_InvalidWeight_ThrowsValidationException()
    {
        await using var db = TestHelpers.CreateDb();
        var service = CreateService(db, TestHelpers.CreateUser());

        var act = async () => await service.UpdatePreferences(
            new UpdateUserPreferencesCommand(1000, null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        db.UserPreferences.Should().BeEmpty();
    }
}
