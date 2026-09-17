using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

// Same "identity.roles" table Identity.Infrastructure's RoleConfiguration owns and seeds
// (HasData) — this side is read-only in practice (Admin's role dropdown), no seed data here to
// avoid two contexts fighting over the same rows. See UserConfiguration's comment.
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "identity", t => t.ExcludeFromMigrations());
        builder.Property(r => r.Name).HasMaxLength(50);
        builder.Property(r => r.DisplayName).HasMaxLength(100);
    }
}
