using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(2);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(255);

        // Seeded 1:1 with FOXLinks' CountrySeeder.
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Country { Id = 1, Name = "Россия", Code = "ru", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Country { Id = 2, Name = "Казахстан", Code = "kz", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Country { Id = 3, Name = "Беларусь", Code = "by", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Country { Id = 4, Name = "Узбекистан", Code = "uz", CreatedAt = seededAt, UpdatedAt = seededAt }
        );
    }
}
