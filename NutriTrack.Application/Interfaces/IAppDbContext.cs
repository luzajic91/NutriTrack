using Microsoft.EntityFrameworkCore;
using NutriTrack.Domain.FoodCatalog;
using NutriTrack.Domain.Identity;
using NutriTrack.Domain.MealLogging;
using NutriTrack.Domain.Recipes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Interfaces
{
    public interface IAppDbContext
    {
        IQueryable<User> Users { get; }
        IQueryable<Role> Roles { get; }
        IQueryable<RefreshToken> RefreshTokens { get; }
        IQueryable<Food> Foods { get; }
        IQueryable<Brand> Brands { get; }
        IQueryable<Nutrient> Nutrients { get; }
        //IQueryable<MeasurementUnit> MeasurementUnits { get; }
        IQueryable<ServingUnit> ServingUnits { get; }
        IQueryable<FoodServing> FoodServings { get; }
        IQueryable<FoodNutrient> FoodNutrients { get; }
        IQueryable<Recipe> Recipes { get; }
        IQueryable<RecipeItem> RecipeItems { get; }
        IQueryable<MealEntry> MealEntries { get; }
        IQueryable<MealEntryItem> MealEntryItems { get; }

        void Add<T>(T entity) where T : class;
        void Remove<T>(T entity) where T : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
