using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Interfaces;
using NutriTrack.Domain.FoodCatalog;
using NutriTrack.Domain.Identity;
using NutriTrack.Domain.MealLogging;
using NutriTrack.Domain.Recipes;

namespace NutriTrack.Infrastructure.Persistence
{
    public class NutriTrackDbContext : DbContext, IAppDbContext
    {
        public NutriTrackDbContext(DbContextOptions<NutriTrackDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Food> Foods => Set<Food>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Nutrient> Nutrients => Set<Nutrient>();
        //public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();
        public DbSet<ServingUnit> ServingUnits => Set<ServingUnit>();
        public DbSet<FoodServing> FoodServings => Set<FoodServing>();
        public DbSet<FoodNutrient> FoodNutrients => Set<FoodNutrient>();
        public DbSet<Recipe> Recipes => Set<Recipe>();
        public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
        public DbSet<MealEntry> MealEntries => Set<MealEntry>();
        public DbSet<MealEntryItem> MealEntryItems => Set<MealEntryItem>();

        IQueryable<User> IAppDbContext.Users => Users;
        IQueryable<Role> IAppDbContext.Roles => Roles;
        IQueryable<RefreshToken> IAppDbContext.RefreshTokens => RefreshTokens;
        IQueryable<Food> IAppDbContext.Foods => Foods;
        IQueryable<Brand> IAppDbContext.Brands => Brands;
        IQueryable<Nutrient> IAppDbContext.Nutrients => Nutrients;
        //IQueryable<MeasurementUnit> IAppDbContext.MeasurementUnits => MeasurementUnits;
        IQueryable<ServingUnit> IAppDbContext.ServingUnits => ServingUnits;
        IQueryable<FoodServing> IAppDbContext.FoodServings => FoodServings;
        IQueryable<FoodNutrient> IAppDbContext.FoodNutrients => FoodNutrients;
        IQueryable<Recipe> IAppDbContext.Recipes => Recipes;
        IQueryable<RecipeItem> IAppDbContext.RecipeItems => RecipeItems;
        IQueryable<MealEntry> IAppDbContext.MealEntries => MealEntries;
        IQueryable<MealEntryItem> IAppDbContext.MealEntryItems => MealEntryItems;

        public void Add<T>(T entity) where T : class => Set<T>().Add(entity);
        public void Remove<T>(T entity) where T : class => Set<T>().Remove(entity);
        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.ApplyConfigurationsFromAssembly(typeof(NutriTrackDbContext).Assembly);
        }
    }
}
