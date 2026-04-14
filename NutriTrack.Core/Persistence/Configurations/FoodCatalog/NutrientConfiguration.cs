namespace NutriTrack.Core.Persistence.Configurations.FoodCatalog;

public class NutrientConfiguration : IEntityTypeConfiguration<Nutrient>
{
    public void Configure(EntityTypeBuilder<Nutrient> b)
    {
        b.ToTable("Nutrients");
        b.HasKey(x => x.NutrientId);
        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.Property(x => x.Abv).HasMaxLength(20).IsRequired();
        b.Property(x => x.MeasurementUnit)
         .HasConversion<int>()
         .IsRequired();
    }
}