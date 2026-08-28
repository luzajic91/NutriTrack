using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using NutriTrack.Shared.Features.Recipes;

namespace NutriTrack.Tests.Features;

public class NutritionParityTests
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
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext!.User).Returns(principal);
        return new CurrentUserService(accessor.Object);
    }

    private static void SeedProteinFood(NutriTrackDbContext db)
    {
        db.Nutrients.Add(new Nutrient { NutrientId = 1, Name = "Protein", Abv = "P", MeasurementUnit = MeasurementUnit.Grams });
        db.Foods.Add(new Food { FoodId = 1, Name = "Chicken" });
        db.FoodNutrients.Add(new FoodNutrient { FoodNutrientId = 1, FoodId = 1, NutrientId = 1, ValuePer100g = 25 });
    }

    [Fact]
    public async Task RecipeNutrition_And_DailySummary_AgreeForSameFoodAndGrams()
    {
        await using var db = CreateDb();
        SeedProteinFood(db);
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 1,
            Name = "Plain Chicken",
            TotalGrams = 200,
            IsPublic = false,
            RecipeItems = [new RecipeItem { FoodId = 1, Grams = 200 }]
        });
        await db.SaveChangesAsync();

        var user = CreateUser();
        var recipes = new RecipeService(db, user, new CreateRecipeValidator(), new UpdateRecipeValidator(), NullLogger<RecipeService>.Instance);
        var meals = new MealLoggingService(db, user, new NutritionQueryService(db), new LogMealValidator(), NullLogger<MealLoggingService>.Instance);

        var consumedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        await meals.LogMeal(new LogMealRequest { Foods = [new MealFoodEntry(1, 200)], ConsumedAt = consumedAt }, CancellationToken.None);

        var recipeNutrition = await recipes.GetRecipeNutrition(1, CancellationToken.None);
        var daily = await meals.GetDailyNutritionSummary(DateOnly.FromDateTime(consumedAt), CancellationToken.None);

        var recipeProtein = recipeNutrition.Nutrients.Single(n => n.Abbreviation == "P").Total;
        var dailyProtein = daily.Nutrients.Single(n => n.Abbreviation == "P").Total;

        recipeProtein.Should().Be(50);          // 25 per 100g * 200g
        dailyProtein.Should().Be(recipeProtein); // single source of truth
    }

    [Fact]
    public async Task SummaryRange_AveragesAcrossDistinctDays()
    {
        await using var db = CreateDb();
        SeedProteinFood(db);
        await db.SaveChangesAsync();

        var user = CreateUser();
        var nutrition = new NutritionQueryService(db);
        var meals = new MealLoggingService(db, user, nutrition, new LogMealValidator(), NullLogger<MealLoggingService>.Instance);

        var day1 = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);
        await meals.LogMeal(new LogMealRequest { Foods = [new MealFoodEntry(1, 200)], ConsumedAt = day1 }, CancellationToken.None); // 50
        await meals.LogMeal(new LogMealRequest { Foods = [new MealFoodEntry(1, 200)], ConsumedAt = day2 }, CancellationToken.None); // 50

        var range = await nutrition.GetSummaryRangeAsync(1, DateOnly.FromDateTime(day1), DateOnly.FromDateTime(day2));

        // total 100 over 2 distinct days => daily average 50
        range.Single(n => n.Abbreviation == "P").Total.Should().Be(50);
    }
}
