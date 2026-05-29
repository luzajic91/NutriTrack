using Microsoft.Extensions.Logging.Abstractions;
using NutriTrack.Shared.Features.FoodCatalog;

namespace NutriTrack.Tests.Features;

public class FoodCatalogServiceTests
{
    private static FoodCatalogService CreateService(NutriTrackDbContext db) =>
        new(db, NullLogger<FoodCatalogService>.Instance);

    [Fact]
    public async Task GetFood_ReturnsNutrientsAndServings()
    {
        await using var db = TestHelpers.CreateDb();
        db.Brands.Add(new Brand { BrandId = 1, Name = "Acme" });
        db.Nutrients.Add(new Nutrient { NutrientId = 1, Name = "Protein", Abv = "P", MeasurementUnit = MeasurementUnit.Grams });
        db.ServingUnits.Add(new ServingUnit { ServingUnitId = 1, Name = "cup" });
        db.Foods.Add(new Food
        {
            FoodId = 1,
            Name = "Rice",
            BrandId = 1,
            Description = "White rice",
            FoodNutrients = [new FoodNutrient { FoodNutrientId = 1, NutrientId = 1, ValuePer100g = 7 }],
            FoodServings = [new FoodServing { FoodServingId = 1, ServingUnitId = 1, DisplayName = "1 cup", GramWeight = 158 }]
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetFood(1, CancellationToken.None);

        result.Name.Should().Be("Rice");
        result.BrandName.Should().Be("Acme");
        result.Nutrients.Should().ContainSingle(n => n.NutrientName == "Protein" && n.ValuePer100g == 7);
        result.Servings.Should().ContainSingle(s => s.DisplayName == "1 cup" && s.ServingUnit == "cup");
    }

    [Fact]
    public async Task GetFood_NonExistent_ThrowsNotFoundException()
    {
        await using var db = TestHelpers.CreateDb();

        var act = async () => await CreateService(db).GetFood(99, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Food 99 not found*");
    }

    [Fact]
    public async Task SearchFoods_AppliesBrandFilterAndPaging()
    {
        await using var db = TestHelpers.CreateDb();
        db.Brands.Add(new Brand { BrandId = 1, Name = "Acme" });
        db.Brands.Add(new Brand { BrandId = 2, Name = "Other" });
        for (var i = 1; i <= 5; i++)
            db.Foods.Add(new Food { FoodId = i, Name = $"Acme Food {i}", BrandId = 1 });
        db.Foods.Add(new Food { FoodId = 6, Name = "Other Food", BrandId = 2 });
        await db.SaveChangesAsync();

        var result = await CreateService(db).SearchFoods(
            search: null, brandId: 1, page: 1, pageSize: 2, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(3);
        result.Items.Should().OnlyContain(f => f.BrandName == "Acme");
    }

    [Fact]
    public async Task SearchFoods_FiltersByNameSubstring()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Brown Rice" });
        db.Foods.Add(new Food { FoodId = 2, Name = "White Rice" });
        db.Foods.Add(new Food { FoodId = 3, Name = "Chicken" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).SearchFoods(
            search: "Rice", brandId: null, page: 1, pageSize: 10, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(f => f.Name.Contains("Rice"));
    }
}
