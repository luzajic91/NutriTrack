namespace NutriTrack.Core.Persistence.Configurations.FoodCatalog;

public class FoodConfiguration : IEntityTypeConfiguration<Food>
{
    public void Configure(EntityTypeBuilder<Food> b)
    {
        b.ToTable("Foods");
        b.HasKey(x => x.FoodId);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(200);
        b.HasOne(x => x.Brand)
         .WithMany()
         .HasForeignKey(x => x.BrandId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}