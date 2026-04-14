namespace NutriTrack.Core.Persistence.Configurations.Identity;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(r => r.RefreshTokenId);
        b.Property(r => r.Token).HasMaxLength(128).IsRequired();
        b.Property(r => r.ReplacedByToken).HasMaxLength(128);
        b.HasOne(r => r.User)
         .WithMany(u => u.RefreshTokens)
         .HasForeignKey(r => r.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}