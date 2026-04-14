namespace NutriTrack.Core.Persistence.Configurations.FoodCatalog;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b)
    {
        b.ToTable("Brands");
        b.HasKey(x => x.BrandId);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
    }
}