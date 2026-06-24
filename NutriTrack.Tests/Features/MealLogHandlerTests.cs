using Microsoft.Extensions.Logging.Abstractions;

namespace NutriTrack.Tests.Features;

public class LogMealHandlerTests
{
    private static MealLoggingService CreateService(NutriTrackDbContext db, CurrentUserService user) =>
        new(db, user, new NutritionQueryService(db), new LogMealValidator(), NullLogger<MealLoggingService>.Instance);

    [Fact]
    public async Task Handle_DirectFoodEntry_PersistsMealWithCorrectGrams()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Chicken" });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser());
        await service.LogMeal(
            new LogMealRequest { Foods = [new MealFoodEntry(1, 150)] },
            CancellationToken.None);

        db.MealEntries.Should().HaveCount(1);
        db.MealEntryItems.First().Grams.Should().Be(150);
    }

    [Fact]
    public async Task Handle_RecipeEntry_ExpandsIntoFoods()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        db.Foods.Add(new Food { FoodId = 2, Name = "Chicken" });
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 1,
            Name = "Bowl",
            TotalGrams = 500,
            IsPublic = false,
            RecipeItems =
            [
                new RecipeItem { FoodId = 1, Grams = 200 },
            new RecipeItem { FoodId = 2, Grams = 300 }
            ]
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser());
        await service.LogMeal(
            new LogMealRequest { Recipes = [new MealRecipeEntry(1, 250)] },
            CancellationToken.None);

        var items = db.MealEntryItems.ToList();
        items.Should().HaveCount(2);
        items.First(i => i.FoodId == 1).Grams.Should().Be(100);  // 200 * 0.5
        items.First(i => i.FoodId == 2).Grams.Should().Be(150);  // 300 * 0.5
    }

    [Fact]
    public async Task Handle_NonExistentFood_ThrowsNotFoundException()
    {
        await using var db = TestHelpers.CreateDb();

        var service = CreateService(db, TestHelpers.CreateUser());
        var act = async () => await service.LogMeal(
            new LogMealRequest { Foods = [new MealFoodEntry(99, 100)] },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Food 99 not found*");
    }

    [Fact]
    public async Task Handle_PrivateRecipeFromOtherUser_ThrowsForbiddenException()
    {
        await using var db = TestHelpers.CreateDb();
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 99,
            Name = "Other",
            TotalGrams = 100,
            IsPublic = false,
            RecipeItems = []
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        var act = async () => await service.LogMeal(
            new LogMealRequest { Recipes = [new MealRecipeEntry(1, 100)] },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
