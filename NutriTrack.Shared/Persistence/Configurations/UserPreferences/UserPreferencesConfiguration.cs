namespace NutriTrack.Shared.Persistence.Configurations.UserPreferences;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<Domain.UserPreferences.UserPreferences>
{
    public void Configure(EntityTypeBuilder<Domain.UserPreferences.UserPreferences> b)
    {
        b.ToTable("UserPreferences");
        b.HasKey(x => x.UserPreferencesId);
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.WeightKg).HasColumnType("decimal(5,2)");
        b.HasOne<User>()
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
