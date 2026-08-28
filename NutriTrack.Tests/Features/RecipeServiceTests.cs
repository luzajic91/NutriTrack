using Microsoft.Extensions.Logging.Abstractions;
using NutriTrack.Shared.Features.Recipes;
using NutriTrack.Shared.Models.Recipes;

namespace NutriTrack.Tests.Features;

public class RecipeServiceTests
{
    private static RecipeService CreateService(NutriTrackDbContext db, CurrentUserService user) =>
        new(db, user, new CreateRecipeValidator(), new UpdateRecipeValidator(), NullLogger<RecipeService>.Instance);

    [Fact]
    public async Task CreateRecipe_SumsItemGramsIntoTotalGrams()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        db.Foods.Add(new Food { FoodId = 2, Name = "Chicken" });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser());
        var id = await service.CreateRecipe(
            new CreateRecipeRequest
            {
                Name = "Bowl",
                Items = [new RecipeItemRequest(1, 200), new RecipeItemRequest(2, 300)]
            },
            CancellationToken.None);

        var recipe = db.Recipes.Single(r => r.RecipeId == id);
        recipe.TotalGrams.Should().Be(500);
    }

    [Fact]
    public async Task CreateRecipe_NonExistentFood_ThrowsNotFoundException()
    {
        await using var db = TestHelpers.CreateDb();

        var service = CreateService(db, TestHelpers.CreateUser());
        var act = async () => await service.CreateRecipe(
            new CreateRecipeRequest
            {
                Name = "Bowl",
                Items = [new RecipeItemRequest(99, 100)]
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Food 99 not found*");
    }

    [Fact]
    public async Task GetRecipe_PrivateRecipeFromOtherUser_ThrowsForbiddenException()
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
        var act = async () => await service.GetRecipe(1, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetRecipe_PublicRecipeFromOtherUser_IsReturned()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 99,
            Name = "Shared",
            TotalGrams = 200,
            IsPublic = true,
            RecipeItems = [new RecipeItem { FoodId = 1, Grams = 200 }]
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        var result = await service.GetRecipe(1, CancellationToken.None);

        result.RecipeId.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.FoodName == "Rice");
    }

    [Fact]
    public async Task DeleteRecipe_NonOwner_ThrowsForbiddenException()
    {
        await using var db = TestHelpers.CreateDb();
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 99,
            Name = "Other",
            TotalGrams = 100,
            IsPublic = true,
            RecipeItems = []
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        var act = async () => await service.DeleteRecipe(1, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        db.Recipes.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecipeNutrition_AggregatesTotalsAndPerServing()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        db.Foods.Add(new Food { FoodId = 2, Name = "Chicken" });
        db.Nutrients.Add(new Nutrient
        {
            NutrientId = 1,
            Name = "Protein",
            Abv = "P",
            MeasurementUnit = MeasurementUnit.Grams
        });
        db.FoodNutrients.Add(new FoodNutrient { FoodNutrientId = 1, FoodId = 1, NutrientId = 1, ValuePer100g = 10 });
        db.FoodNutrients.Add(new FoodNutrient { FoodNutrientId = 2, FoodId = 2, NutrientId = 1, ValuePer100g = 20 });
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 1,
            Name = "Bowl",
            TotalGrams = 500,
            ServingsCount = 2,
            IsPublic = false,
            RecipeItems = [new RecipeItem { FoodId = 1, Grams = 200 }, new RecipeItem { FoodId = 2, Grams = 300 }]
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        var result = await service.GetRecipeNutrition(1, CancellationToken.None);

        // 10 * 200/100 + 20 * 300/100 = 20 + 60 = 80
        var protein = result.Nutrients.Single(n => n.Name == "Protein");
        protein.Total.Should().Be(80);
        protein.Unit.Should().Be("Grams");

        // per serving (2 servings) => 40
        result.NutrientsPerServing.Should().NotBeNull();
        result.NutrientsPerServing!.Single(n => n.Name == "Protein").Total.Should().Be(40);
    }

    [Fact]
    public async Task UpdateRecipe_ReplacesItemsAndRecomputesTotalGrams()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        db.Foods.Add(new Food { FoodId = 2, Name = "Chicken" });
        db.Foods.Add(new Food { FoodId = 3, Name = "Broccoli" });
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 1,
            Name = "Bowl",
            TotalGrams = 500,
            IsPublic = false,
            RecipeItems = [new RecipeItem { FoodId = 1, Grams = 200 }, new RecipeItem { FoodId = 2, Grams = 300 }]
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        await service.UpdateRecipe(
            1,
            new UpdateRecipeRequest
            {
                Name = "Veggie Bowl",
                Description = "Now meatless",
                ServingsCount = 3,
                IsPublic = true,
                Items = [new RecipeItemRequest(1, 150), new RecipeItemRequest(3, 100)]
            },
            CancellationToken.None);

        var recipe = db.Recipes.Include(r => r.RecipeItems).Single(r => r.RecipeId == 1);
        recipe.Name.Should().Be("Veggie Bowl");
        recipe.Description.Should().Be("Now meatless");
        recipe.ServingsCount.Should().Be(3);
        recipe.IsPublic.Should().BeTrue();

        // 150 + 100 — derived from the new items, not carried over from the old 500.
        recipe.TotalGrams.Should().Be(250);

        recipe.RecipeItems.Should().HaveCount(2);
        recipe.RecipeItems.Select(i => i.FoodId).Should().BeEquivalentTo(new[] { 1, 3 });
        db.RecipeItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateRecipe_NonOwner_ThrowsForbiddenException()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 99,
            Name = "Other",
            TotalGrams = 100,
            // Public: visible to everyone, still editable only by its owner.
            IsPublic = true,
            RecipeItems = [new RecipeItem { FoodId = 1, Grams = 100 }]
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        var act = async () => await service.UpdateRecipe(
            1,
            new UpdateRecipeRequest { Name = "Hijacked", Items = [new RecipeItemRequest(1, 999)] },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();

        var recipe = db.Recipes.Single(r => r.RecipeId == 1);
        recipe.Name.Should().Be("Other");
        recipe.TotalGrams.Should().Be(100);
    }

    [Fact]
    public async Task UpdateRecipe_NonExistentRecipe_ThrowsNotFoundException()
    {
        await using var db = TestHelpers.CreateDb();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        var act = async () => await service.UpdateRecipe(
            42,
            new UpdateRecipeRequest { Name = "Bowl", Items = [new RecipeItemRequest(1, 100)] },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Recipe 42 not found*");
    }

    [Fact]
    public async Task UpdateRecipe_NonExistentFood_ThrowsNotFoundException()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 1,
            Name = "Bowl",
            TotalGrams = 100,
            IsPublic = false,
            RecipeItems = [new RecipeItem { FoodId = 1, Grams = 100 }]
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        var act = async () => await service.UpdateRecipe(
            1,
            new UpdateRecipeRequest { Name = "Bowl", Items = [new RecipeItemRequest(99, 100)] },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Food 99 not found*");
    }

    [Fact]
    public async Task UpdateRecipe_EmptyItems_ThrowsValidationException()
    {
        await using var db = TestHelpers.CreateDb();
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 1,
            Name = "Bowl",
            TotalGrams = 100,
            IsPublic = false,
            RecipeItems = []
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, TestHelpers.CreateUser(userId: 1));
        var act = async () => await service.UpdateRecipe(
            1,
            new UpdateRecipeRequest { Name = "Bowl", Items = [] },
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task GetRecipe_SetsIsOwnerForTheOwnerOnly()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        db.Recipes.Add(new Recipe
        {
            RecipeId = 1,
            UserId = 1,
            Name = "Mine",
            TotalGrams = 100,
            IsPublic = true,
            RecipeItems = [new RecipeItem { FoodId = 1, Grams = 100 }]
        });
        await db.SaveChangesAsync();

        var owner = CreateService(db, TestHelpers.CreateUser(userId: 1));
        (await owner.GetRecipe(1, CancellationToken.None)).IsOwner.Should().BeTrue();

        var otherUser = CreateService(db, TestHelpers.CreateUser(userId: 2));
        (await otherUser.GetRecipe(1, CancellationToken.None)).IsOwner.Should().BeFalse();
    }
}
