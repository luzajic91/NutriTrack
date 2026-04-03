using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriTrack.Domain.FoodCatalog;
using NutriTrack.Domain.MealLogging;

namespace NutriTrack.Infrastructure.Persistence.MealLogging
{
    public class MealEntryItemConfiguration : IEntityTypeConfiguration<MealEntryItem>
    {
        public void Configure(EntityTypeBuilder<MealEntryItem> b)
        {
            b.ToTable("MealEntryItems");
            b.HasKey(x => x.MealEntryItemId);
            b.Property(x => x.Grams).HasColumnType("decimal(10,2)").IsRequired();
            b.HasOne(x => x.MealEntry)
             .WithMany(m => m.Items)
             .HasForeignKey(x => x.MealEntryId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Food>()
             .WithMany()
             .HasForeignKey(x => x.FoodId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
