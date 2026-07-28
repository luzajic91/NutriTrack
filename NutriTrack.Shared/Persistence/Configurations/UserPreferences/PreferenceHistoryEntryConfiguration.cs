using NutriTrack.Domain.UserPreferences;

namespace NutriTrack.Shared.Persistence.Configurations.UserPreferences;

public class PreferenceHistoryEntryConfiguration : IEntityTypeConfiguration<PreferenceHistoryEntry>
{
    public void Configure(EntityTypeBuilder<PreferenceHistoryEntry> b)
    {
        b.ToTable("PreferenceHistory");
        b.HasKey(x => x.PreferenceHistoryEntryId);
        b.Property(x => x.Metric).HasConversion<int>().IsRequired();
        b.Property(x => x.Value).HasColumnType("decimal(10,2)").IsRequired();
        b.Property(x => x.RecordedAt).IsRequired();
        b.HasIndex(x => new { x.UserId, x.Metric, x.RecordedAt });
        b.HasOne(x => x.User)
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
