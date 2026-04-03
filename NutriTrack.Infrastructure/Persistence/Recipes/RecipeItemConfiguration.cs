using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriTrack.Domain.FoodCatalog;
using NutriTrack.Domain.Recipes;

namespace NutriTrack.Infrastructure.Persistence.Recipes
{
    public class RecipeItemConfiguration : IEntityTypeConfiguration<RecipeItem>
    {
        public void Configure(EntityTypeBuilder<RecipeItem> b)
        {
            b.ToTable("RecipeItems");
            b.HasKey(x => x.RecipeItemId);
            b.Property(x => x.Grams).HasColumnType("decimal(10,2)").IsRequired();
            b.HasOne(x => x.Recipe)
             .WithMany(r => r.RecipeItems)
             .HasForeignKey(x => x.RecipeId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Food>()
             .WithMany()
             .HasForeignKey(x => x.FoodId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
