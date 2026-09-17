using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.Property(r => r.DisplayName).HasMaxLength(100);

        // Seeded 1:1 with FOXLinks' roles migration (2025_12_05_105222_create_roles_and_user_role_table.php).
        // CreatedAt/UpdatedAt are fixed (not BaseEntity's DateTime.UtcNow default) — HasData is
        // baked into the migration snapshot, so a "now" default would make the model appear to
        // change on every `dotnet ef migrations add`.
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Role { Id = 1, Name = "admin", DisplayName = "Администратор", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Role { Id = 2, Name = "moderator", DisplayName = "Модератор", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Role { Id = 3, Name = "webmaster", DisplayName = "Вебмастер", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Role { Id = 4, Name = "universal", DisplayName = "Универсальный", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Role { Id = 5, Name = "optimizer", DisplayName = "Оптимизатор", CreatedAt = seededAt, UpdatedAt = seededAt }
        );
    }
}
