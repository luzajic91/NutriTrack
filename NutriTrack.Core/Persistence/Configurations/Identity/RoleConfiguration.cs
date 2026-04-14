namespace NutriTrack.Core.Persistence.Configurations.Identity;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(r => r.RoleId);
        b.Property(r => r.Name).HasMaxLength(50).IsRequired();
    }
}