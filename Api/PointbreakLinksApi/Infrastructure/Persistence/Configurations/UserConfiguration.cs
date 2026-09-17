using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

// Maps the SAME "identity.users" table Identity.Infrastructure's UserConfiguration owns —
// ExcludeFromMigrations means this context never generates CREATE/ALTER/DROP DDL for it (or
// for the "roles"/"role_user" tables below), only Identity's migrations do. Runtime reads and
// writes (Admin ban/role management, Site.Seller/Project.User/etc. navigation) work normally;
// only schema ownership is exclusive to Identity. See PROJECT_MAP.md for the full design.
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity", t => t.ExcludeFromMigrations());
        builder.Property(u => u.Name).HasMaxLength(255);
        builder.Property(u => u.Email).HasMaxLength(255);

        builder.HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity(j => j.ToTable("role_user", "identity", t => t.ExcludeFromMigrations()));

        builder.HasMany(u => u.Projects)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
