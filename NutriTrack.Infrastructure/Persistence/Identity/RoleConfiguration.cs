using NutriTrack.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NutriTrack.Infrastructure.Persistence.Identity
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> b)
        {
            b.ToTable("Roles");
            b.HasKey(r => r.RoleId);
            b.Property(r => r.Name).HasMaxLength(50).IsRequired();
        }
    }
}
