using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NutriTrack.Shared.Caching;
using NutriTrack.Shared.Features.FoodCatalog;
using NutriTrack.Shared.Models.Foods;

namespace NutriTrack.Tests.Features;

public class FoodCatalogServiceTests
{
    /// <summary>The page size the food dropdowns actually request, and one of the cached sizes.</summary>
    private const int UiPageSize = 50;

    private static FoodCatalogService CreateService(
        NutriTrackDbContext db, HybridCache? cache = null)
    {
        cache ??= TestHelpers.CreateCache();
        return new FoodCatalogService(
            db,
            cache,
            new ReferenceDataCache(cache, db),
            new SearchFoodsValidator(),
            NullLogger<FoodCatalogService>.Instance);
    }

    private static SearchFoodsRequest Search(
        string? search = null, int? brandId = null, int page = 1, int pageSize = 20) =>
        new() { Search = search, BrandId = brandId, Page = page, PageSize = pageSize };

    private static async Task SeedRiceAsync(NutriTrackDbContext db)
    {
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
    }

    [Fact]
    public async Task GetFood_ReturnsNutrientsAndServings()
    {
        await using var db = TestHelpers.CreateDb();
        await SeedRiceAsync(db);

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
    public async Task GetFood_NonExistent_IsNotCachedAsAFailure()
    {
        await using var db = TestHelpers.CreateDb();
        var cache = TestHelpers.CreateCache();

        var missing = async () => await CreateService(db, cache).GetFood(1, CancellationToken.None);
        await missing.Should().ThrowAsync<NotFoundException>();

        // A miss must not poison the key: once the food exists the next read has to find it.
        await SeedRiceAsync(db);
        var result = await CreateService(db, cache).GetFood(1, CancellationToken.None);

        result.Name.Should().Be("Rice");
    }

    [Fact]
    public async Task GetFood_SecondCall_IsServedFromCacheWithoutReadingTheDatabase()
    {
        await using var db = TestHelpers.CreateDb();
        await SeedRiceAsync(db);
        var cache = TestHelpers.CreateCache();

        await CreateService(db, cache).GetFood(1, CancellationToken.None);

        // Renaming the row behind the cache's back is the only way to prove the second call
        // never went to the database.
        db.Foods.Single(f => f.FoodId == 1).Name = "Renamed";
        await db.SaveChangesAsync();

        var cached = await CreateService(db, cache).GetFood(1, CancellationToken.None);

        cached.Name.Should().Be("Rice");
    }

    [Fact]
    public async Task GetFood_AfterFoodsTagIsInvalidated_ReadsAgain()
    {
        await using var db = TestHelpers.CreateDb();
        await SeedRiceAsync(db);
        var cache = TestHelpers.CreateCache();

        await CreateService(db, cache).GetFood(1, CancellationToken.None);
        db.Foods.Single(f => f.FoodId == 1).Name = "Renamed";
        await db.SaveChangesAsync();

        await cache.RemoveByTagAsync(CacheTags.Foods, CancellationToken.None);
        var result = await CreateService(db, cache).GetFood(1, CancellationToken.None);

        result.Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task GetFood_WhenCachedReferenceDataPredatesANutrient_ReloadsItRatherThanDroppingIt()
    {
        await using var db = TestHelpers.CreateDb();
        await SeedRiceAsync(db);
        var cache = TestHelpers.CreateCache();

        // Warm the reference snapshot while it holds only Protein.
        await new ReferenceDataCache(cache, db).GetAsync(CancellationToken.None);

        db.Nutrients.Add(new Nutrient
        {
            NutrientId = 2, Name = "Fat", Abv = "F", MeasurementUnit = MeasurementUnit.Grams
        });
        db.FoodNutrients.Add(new FoodNutrient
        {
            FoodNutrientId = 2, FoodId = 1, NutrientId = 2, ValuePer100g = 3
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db, cache).GetFood(1, CancellationToken.None);

        // The stale snapshot knew nothing of Fat; silently omitting it would be data loss.
        result.Nutrients.Should().HaveCount(2);
        result.Nutrients.Should().ContainSingle(n => n.NutrientName == "Fat" && n.ValuePer100g == 3);
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
            Search(brandId: 1, pageSize: 2), CancellationToken.None);

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
            Search(search: "Rice", pageSize: 10), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(f => f.Name.Contains("Rice"));
    }

    [Fact]
    public async Task SearchFoods_BrowsePage_IsCached()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        await db.SaveChangesAsync();
        var cache = TestHelpers.CreateCache();

        await CreateService(db, cache).SearchFoods(Search(), CancellationToken.None);

        db.Foods.Add(new Food { FoodId = 2, Name = "Chicken" });
        await db.SaveChangesAsync();

        var cached = await CreateService(db, cache).SearchFoods(Search(), CancellationToken.None);

        cached.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchFoods_AtThePageSizeTheUiRequests_IsCached()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        await db.SaveChangesAsync();
        var cache = TestHelpers.CreateCache();

        await CreateService(db, cache).SearchFoods(
            Search(pageSize: UiPageSize), CancellationToken.None);

        db.Foods.Add(new Food { FoodId = 2, Name = "Chicken" });
        await db.SaveChangesAsync();

        var cached = await CreateService(db, cache).SearchFoods(
            Search(pageSize: UiPageSize), CancellationToken.None);

        // The dropdowns ask for 50, not the API default. If only the default were cached the
        // cache would be dead weight for the one client that exists.
        cached.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchFoods_AtAnUnusualPageSize_IsNotCached()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        await db.SaveChangesAsync();
        var cache = TestHelpers.CreateCache();

        await CreateService(db, cache).SearchFoods(Search(pageSize: 7), CancellationToken.None);

        db.Foods.Add(new Food { FoodId = 2, Name = "Chicken" });
        await db.SaveChangesAsync();

        var fresh = await CreateService(db, cache).SearchFoods(
            Search(pageSize: 7), CancellationToken.None);

        // Validation bounds pageSize to 1..100, but caching all hundred values times every page
        // times every brand is a lot of entries nothing reads twice. Only the sizes the app
        // actually uses are kept; the rest are served fresh.
        fresh.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchFoods_DeepIntoTheCatalog_IsNotCached()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        await db.SaveChangesAsync();
        var cache = TestHelpers.CreateCache();

        await CreateService(db, cache).SearchFoods(Search(page: 50), CancellationToken.None);

        db.Foods.Add(new Food { FoodId = 2, Name = "Chicken" });
        await db.SaveChangesAsync();

        var fresh = await CreateService(db, cache).SearchFoods(
            Search(page: 50), CancellationToken.None);

        fresh.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchFoods_FreeTextQuery_IsNotCached()
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Brown Rice" });
        await db.SaveChangesAsync();
        var cache = TestHelpers.CreateCache();

        await CreateService(db, cache).SearchFoods(
            Search(search: "Rice"), CancellationToken.None);

        db.Foods.Add(new Food { FoodId = 2, Name = "White Rice" });
        await db.SaveChangesAsync();

        // Caller-controlled keys are unbounded, so these queries always hit the database.
        var fresh = await CreateService(db, cache).SearchFoods(
            Search(search: "Rice"), CancellationToken.None);

        fresh.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchFoods_DifferentPageSizes_DoNotShareACacheEntry()
    {
        await using var db = TestHelpers.CreateDb();
        for (var i = 1; i <= 30; i++)
            db.Foods.Add(new Food { FoodId = i, Name = $"Food {i:D2}" });
        await db.SaveChangesAsync();
        var service = CreateService(db, TestHelpers.CreateCache());

        var twenty = await service.SearchFoods(Search(pageSize: 20), CancellationToken.None);
        var fifty = await service.SearchFoods(Search(pageSize: UiPageSize), CancellationToken.None);

        // Both sizes are cached, so a key that ignored pageSize would serve one for the other.
        twenty.Items.Should().HaveCount(20);
        fifty.Items.Should().HaveCount(30);
    }

    [Fact]
    public async Task SearchFoods_PagingAndBrandFilter_AreIndependent()
    {
        await using var db = TestHelpers.CreateDb();
        db.Brands.Add(new Brand { BrandId = 1, Name = "Acme" });
        for (var i = 1; i <= 3; i++)
            db.Foods.Add(new Food { FoodId = i, Name = $"Food {i}", BrandId = 1 });
        await db.SaveChangesAsync();
        var service = CreateService(db, TestHelpers.CreateCache());

        var page1 = await service.SearchFoods(Search(page: 1, pageSize: 2), CancellationToken.None);
        var page2 = await service.SearchFoods(Search(page: 2, pageSize: 2), CancellationToken.None);
        var branded = await service.SearchFoods(
            Search(brandId: 1, pageSize: 2), CancellationToken.None);

        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(1);
        page2.Items[0].Name.Should().Be("Food 3");
        branded.Items.Should().OnlyContain(f => f.BrandName == "Acme");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchFoods_PageBelowOne_IsRejected(int page)
    {
        await using var db = TestHelpers.CreateDb();

        var act = async () => await CreateService(db).SearchFoods(
            Search(page: page), CancellationToken.None);

        // page=0 used to reach SQL Server as OFFSET -20 and return 500.
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(99999)]
    public async Task SearchFoods_PageSizeOutsideTheAllowedRange_IsRejected(int pageSize)
    {
        await using var db = TestHelpers.CreateDb();

        var act = async () => await CreateService(db).SearchFoods(
            Search(pageSize: pageSize), CancellationToken.None);

        // 99999 previously returned the entire catalog in one response.
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(SearchFoodsValidator.MaxPageSize)]
    public async Task SearchFoods_PageSizeAtTheBoundary_IsAccepted(int pageSize)
    {
        await using var db = TestHelpers.CreateDb();
        db.Foods.Add(new Food { FoodId = 1, Name = "Rice" });
        await db.SaveChangesAsync();

        var act = async () => await CreateService(db).SearchFoods(
            Search(pageSize: pageSize), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SearchFoods_NonPositiveBrandId_IsRejected()
    {
        await using var db = TestHelpers.CreateDb();

        var act = async () => await CreateService(db).SearchFoods(
            Search(brandId: 0), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task SearchFoods_OverlongSearchTerm_IsRejected()
    {
        await using var db = TestHelpers.CreateDb();

        var act = async () => await CreateService(db).SearchFoods(
            Search(search: new string('x', 101)), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
