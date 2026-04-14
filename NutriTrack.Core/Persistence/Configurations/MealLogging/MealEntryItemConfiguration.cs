namespace NutriTrack.Core.Persistence.Configurations.MealLogging;

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