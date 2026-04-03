using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriTrack.Domain.FoodCatalog;

namespace NutriTrack.Infrastructure.Persistence.FoodCatalog
{
    public class BrandConfiguration : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> b)
        {
            b.ToTable("Brands");
            b.HasKey(x => x.BrandId);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        }
    }
}
