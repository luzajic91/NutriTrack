namespace NutriTrack.Core.Persistence;

public class NutriTrackDbContext : DbContext
{
    public NutriTrackDbContext(DbContextOptions<NutriTrackDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Nutrient> Nutrients => Set<Nutrient>();
    public DbSet<ServingUnit> ServingUnits => Set<ServingUnit>();
    public DbSet<FoodServing> FoodServings => Set<FoodServing>();
    public DbSet<FoodNutrient> FoodNutrients => Set<FoodNutrient>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    public DbSet<MealEntryItem> MealEntryItems => Set<MealEntryItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(NutriTrackDbContext).Assembly);
    }
}