namespace NutriTrack.Core.Persistence.Configurations.FoodCatalog;

public class FoodNutrientConfiguration : IEntityTypeConfiguration<FoodNutrient>
{
    public void Configure(EntityTypeBuilder<FoodNutrient> b)
    {
        b.ToTable("FoodNutrients");
        b.HasKey(x => x.FoodNutrientId);
        b.Property(x => x.ValuePer100g).HasColumnType("decimal(10,2)").IsRequired();
        b.HasOne(x => x.Food)
         .WithMany(f => f.FoodNutrients)
         .HasForeignKey(x => x.FoodId)
         .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Nutrient)
         .WithMany()
         .HasForeignKey(x => x.NutrientId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}