using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Tests.Features
{
    public class MealLogHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IDailySummaryCacheService> _cache = new();
        private readonly LogMealHandler _handler;

        public MealLogHandlerTests()
        {
            _currentUser.Setup(x => x.UserId).Returns(1);
            _handler = new LogMealHandler(_db.Object, _currentUser.Object, _cache.Object);
        }

        [Fact]
        public async Task Handle_DirectFoodEntry_PersistsMealWithCorrectGrams()
        {
            // Arrange
            var foods = new List<Food> { new() { FoodId = 1, Name = "Chicken" } };
            var savedEntries = new List<MealEntry>();

            _db.Setup(x => x.Foods)
                .Returns(MockDbSet.CreateMockQueryable(foods));
            _db.Setup(x => x.Add(It.IsAny<MealEntry>()))
                .Callback<MealEntry>(e => savedEntries.Add(e));
            _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var cmd = new LogMealCommand(
                Foods: [new MealFoodEntry(FoodId: 1, Grams: 150)],
                Recipes: [],
                ConsumedAt: null);

            // Act
            await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            savedEntries.Should().HaveCount(1);
            savedEntries[0].Items.Should().HaveCount(1);
            savedEntries[0].Items.First().Grams.Should().Be(150);
            savedEntries[0].Items.First().FoodId.Should().Be(1);
        }

        [Fact]
        public async Task Handle_RecipeEntry_ExpandsIntoFoods()
        {
            // Arrange
            var recipe = new Recipe
            {
                RecipeId = 1,
                UserId = 1,
                TotalGrams = 500,
                IsPublic = false,
                RecipeItems =
                [
                    new RecipeItem { FoodId = 1, Grams = 200 },
                new RecipeItem { FoodId = 2, Grams = 300 }
                ]
            };

            var savedEntries = new List<MealEntry>();

            _db.Setup(x => x.Foods)
                .Returns(MockDbSet.CreateMockQueryable(new List<Food>()));
            _db.Setup(x => x.Recipes)
                .Returns(MockDbSet.CreateMockQueryable(new List<Recipe> { recipe }));
            _db.Setup(x => x.Add(It.IsAny<MealEntry>()))
                .Callback<MealEntry>(e => savedEntries.Add(e));
            _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // user logs 250g of a 500g recipe — expects 50% scale
            var cmd = new LogMealCommand(
                Foods: [],
                Recipes: [new MealRecipeEntry(RecipeId: 1, Grams: 250)],
                ConsumedAt: null);

            // Act
            await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            var items = savedEntries[0].Items.ToList();
            items.Should().HaveCount(2);
            items[0].Grams.Should().Be(100); // 200 * 0.5
            items[1].Grams.Should().Be(150); // 300 * 0.5
        }

        [Fact]
        public async Task Handle_NonExistentFood_ThrowsNotFoundException()
        {
            // Arrange
            _db.Setup(x => x.Foods)
                .Returns(MockDbSet.CreateMockQueryable(new List<Food>()));

            var cmd = new LogMealCommand(
                Foods: [new MealFoodEntry(FoodId: 99, Grams: 100)],
                Recipes: [],
                ConsumedAt: null);

            // Act
            var act = async () => await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Food 99 not found*");
        }

        [Fact]
        public async Task Handle_PrivateRecipeFromOtherUser_ThrowsForbiddenException()
        {
            // Arrange
            var recipe = new Recipe
            {
                RecipeId = 1,
                UserId = 99, // different user
                IsPublic = false,
                TotalGrams = 100,
                RecipeItems = []
            };

            _db.Setup(x => x.Foods)
                .Returns(MockDbSet.CreateMockQueryable(new List<Food>()));
            _db.Setup(x => x.Recipes)
                .Returns(MockDbSet.CreateMockQueryable(new List<Recipe> { recipe }));

            var cmd = new LogMealCommand(
                Foods: [],
                Recipes: [new MealRecipeEntry(RecipeId: 1, Grams: 100)],
                ConsumedAt: null);

            // Act
            var act = async () => await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }
    }
}
