namespace NutriTrack.Core.Persistence.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.UserId);
        b.Property(u => u.Email).HasMaxLength(255).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        b.HasOne(u => u.Role)
         .WithMany()
         .HasForeignKey(u => u.RoleId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}