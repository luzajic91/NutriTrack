using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriTrack.Domain.FoodCatalog;

namespace NutriTrack.Infrastructure.Persistence.FoodCatalog
{
    public class NutrientConfiguration : IEntityTypeConfiguration<Nutrient>
    {
        public void Configure(EntityTypeBuilder<Nutrient> b)
        {
            b.ToTable("Nutrients");
            b.HasKey(x => x.NutrientId);
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.Property(x => x.Abv).HasMaxLength(20).IsRequired();
            b.HasOne(x => x.MeasurementUnit)
             .WithMany()
             .HasForeignKey(x => x.MeasurementUnitId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
