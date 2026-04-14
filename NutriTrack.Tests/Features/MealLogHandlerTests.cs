using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace NutriTrack.Tests.Features;

public class LogMealHandlerTests
{
    private static NutriTrackDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NutriTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NutriTrackDbContext(options);
    }

    private static CurrentUserService CreateUser(int userId = 1)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                             new Claim(ClaimTypes.Role, "User") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext!.User).Returns(principal);
        return new CurrentUserService(accessor.Object);
    }

    [Fact]
    public async Task Handle_DirectFoodEntry_PersistsMealWithCorrectGrams()
    {
        await using var db = CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Chicken" });
        await db.SaveChangesAsync();

        var handler = new LogMealHandler(db, CreateUser());
        await handler.Handle(
            new LogMealCommand([new MealFoodEntry(1, 150)], [], null),
            CancellationToken.None);

        db.MealEntries.Should().HaveCount(1);
        db.MealEntryItems.First().Grams.Should().Be(150);
    }

    [Fact]
    public async Task Handle_RecipeEntry_ExpandsIntoFoods()
    {
        await using var db = CreateDb();
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

        var handler = new LogMealHandler(db, CreateUser());
        await handler.Handle(
            new LogMealCommand([], [new MealRecipeEntry(1, 250)], null),
            CancellationToken.None);

        var items = db.MealEntryItems.ToList();
        items.Should().HaveCount(2);
        items.First(i => i.FoodId == 1).Grams.Should().Be(100);  // 200 * 0.5
        items.First(i => i.FoodId == 2).Grams.Should().Be(150);  // 300 * 0.5
    }

    [Fact]
    public async Task Handle_NonExistentFood_ThrowsNotFoundException()
    {
        await using var db = CreateDb();

        var handler = new LogMealHandler(db, CreateUser());
        var act = async () => await handler.Handle(
            new LogMealCommand([new MealFoodEntry(99, 100)], [], null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Food 99 not found*");
    }

    [Fact]
    public async Task Handle_PrivateRecipeFromOtherUser_ThrowsForbiddenException()
    {
        await using var db = CreateDb();
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

        var handler = new LogMealHandler(db, CreateUser(userId: 1));
        var act = async () => await handler.Handle(
            new LogMealCommand([], [new MealRecipeEntry(1, 100)], null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}