namespace NutriTrack.Shared.Persistence;

public class NutriTrackDbContext : DbContext
{
    public NutriTrackDbContext(DbContextOptions<NutriTrackDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();
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
    public DbSet<Domain.UserPreferences.UserPreferences> UserPreferences => Set<Domain.UserPreferences.UserPreferences>();
    public DbSet<Domain.UserPreferences.PreferenceHistoryEntry> PreferenceHistory => Set<Domain.UserPreferences.PreferenceHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(NutriTrackDbContext).Assembly);
    }
}
