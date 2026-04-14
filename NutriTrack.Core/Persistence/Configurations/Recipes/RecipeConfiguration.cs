namespace NutriTrack.Core.Persistence.Configurations.Recipes;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> b)
    {
        b.ToTable("Recipes");
        b.HasKey(x => x.RecipeId);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(200);
        b.Property(x => x.TotalGrams).HasColumnType("decimal(10,2)").IsRequired();
        b.HasOne<User>()
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}