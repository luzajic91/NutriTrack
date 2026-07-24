namespace NutriTrack.Shared.Persistence.Configurations.Identity;

public class EmailConfirmationTokenConfiguration : IEntityTypeConfiguration<EmailConfirmationToken>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationToken> b)
    {
        b.ToTable("EmailConfirmationTokens");
        b.HasKey(t => t.EmailConfirmationTokenId);
        b.Property(t => t.Token).HasMaxLength(128).IsRequired();
        b.HasIndex(t => t.Token);
        b.HasOne(t => t.User)
         .WithMany(u => u.EmailConfirmationTokens)
         .HasForeignKey(t => t.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
