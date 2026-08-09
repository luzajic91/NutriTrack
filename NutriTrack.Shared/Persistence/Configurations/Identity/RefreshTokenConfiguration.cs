namespace NutriTrack.Shared.Persistence.Configurations.Identity;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(r => r.RefreshTokenId);
        // Stores a SHA-256 hex hash, not the token itself. 128 leaves room to spare.
        b.Property(r => r.Token).HasMaxLength(128).IsRequired();
        b.Property(r => r.ReplacedByToken).HasMaxLength(128);

        // Every refresh and revoke looks a token up by this column; without an index each one
        // is a table scan that grows with the number of sessions ever issued.
        b.HasIndex(r => r.Token);
        b.HasOne(r => r.User)
         .WithMany(u => u.RefreshTokens)
         .HasForeignKey(r => r.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
