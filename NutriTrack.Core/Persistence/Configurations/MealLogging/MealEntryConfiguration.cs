namespace NutriTrack.Core.Persistence.Configurations.MealLogging;

public class MealEntryConfiguration : IEntityTypeConfiguration<MealEntry>
{
    public void Configure(EntityTypeBuilder<MealEntry> b)
    {
        b.ToTable("MealEntries");
        b.HasKey(x => x.MealEntryId);
        b.Property(x => x.ConsumedAt).HasColumnType("datetime2(0)").IsRequired();
        b.HasOne<User>()
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}