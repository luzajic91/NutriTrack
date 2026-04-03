using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriTrack.Domain.FoodCatalog;

namespace NutriTrack.Infrastructure.Persistence.FoodCatalog
{
    public class FoodServingConfiguration : IEntityTypeConfiguration<FoodServing>
    {
        public void Configure(EntityTypeBuilder<FoodServing> b)
        {
            b.ToTable("FoodServings");
            b.HasKey(x => x.FoodServingId);
            b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            b.Property(x => x.GramWeight).HasColumnType("decimal(10,2)").IsRequired();
            b.HasOne(x => x.Food)
             .WithMany(f => f.FoodServings)
             .HasForeignKey(x => x.FoodId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.ServingUnit)
             .WithMany()
             .HasForeignKey(x => x.ServingUnitId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
