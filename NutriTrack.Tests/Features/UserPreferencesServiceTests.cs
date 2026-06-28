using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NutriTrack.Shared.Features.UserPreferences;
using NutriTrack.Shared.Models.UserPreferences;

namespace NutriTrack.Tests.Features;

public class UserPreferencesServiceTests
{
    private static UserPreferencesService CreateService(NutriTrackDbContext db, CurrentUserService user) =>
        new(db, user, new UpdateUserPreferencesValidator(), NullLogger<UserPreferencesService>.Instance);

    [Fact]
    public async Task GetAsync_NoRow_ReturnsNullSafeResponse()
    {
        await using var db = TestHelpers.CreateDb();

        var result = await CreateService(db, TestHelpers.CreateUser()).GetAsync(CancellationToken.None);

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

        await service.UpdateAsync(new UpdateUserPreferencesRequest { WeightKg = 80, CalorieGoal = 2000, ProteinGoalG = 150, CarbGoalG = 200, FatGoalG = 65 }, CancellationToken.None);
        await service.UpdateAsync(new UpdateUserPreferencesRequest { WeightKg = 90, CalorieGoal = 2100, ProteinGoalG = 160, CarbGoalG = 210, FatGoalG = 70 }, CancellationToken.None);

        db.UserPreferences.Should().HaveCount(1);
        var saved = await service.GetAsync(CancellationToken.None);
        saved.WeightKg.Should().Be(90);
        saved.CalorieGoal.Should().Be(2100);
    }

    [Fact]
    public async Task UpdateAsync_InvalidWeight_ThrowsValidationException()
    {
        await using var db = TestHelpers.CreateDb();
        var service = CreateService(db, TestHelpers.CreateUser());

        var act = async () => await service.UpdateAsync(
            new UpdateUserPreferencesRequest { WeightKg = 1000 }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        db.UserPreferences.Should().BeEmpty();
    }
}
