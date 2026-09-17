using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DynamicStatConfiguration : IEntityTypeConfiguration<DynamicStat>
{
    public void Configure(EntityTypeBuilder<DynamicStat> builder)
    {
        builder.ToTable("dynamic_stats");
        builder.HasIndex(d => d.PageKey);
        builder.HasIndex(d => new { d.PageKey, d.Position }).IsUnique();
        builder.Property(d => d.PageKey).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Title).IsRequired();
        builder.Property(d => d.Value).IsRequired();

        // Ported 1:1 from FOXLinks' database/seeders/DynamicStatsSeeder.php (titles, initial
        // value "0", ₽ suffix only where the seeder's getInitialSuffix() puts one). Every
        // position now has a real IDynamicStatsRefresher method behind it (see that interface's
        // comments) — FOXLinks itself only ever wires up positions 1 (all three pages) and 2
        // (webmaster only) via its Observers; positions 3/4 sit at "0" forever there. Rather than
        // reproduce that as a decorative dead card, this port computes all twelve for real from
        // existing PurchasedSite/Site data (see PROJECT_MAP.md's "Что уже готово").
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DynamicStat Seed(int id, string pageKey, int position, string title, string? valueSuffix) => new()
        {
            Id = id,
            PageKey = pageKey,
            Position = position,
            Title = title,
            Value = "0",
            ValueSuffix = valueSuffix,
            CreatedAt = seededAt,
            UpdatedAt = seededAt,
        };

        builder.HasData(
            Seed(1, "webmaster", 1, "Всего площадок", null),
            Seed(2, "webmaster", 2, "Активных продаж", null),
            Seed(3, "webmaster", 3, "Общий доход", "₽"),
            Seed(4, "webmaster", 4, "Средняя цена", "₽"),
            Seed(5, "optimizator", 1, "Доступно площадок", null),
            Seed(6, "optimizator", 2, "Активные заказы", null),
            Seed(7, "optimizator", 3, "Средняя цена", "₽"),
            Seed(8, "optimizator", 4, "Экономия", "₽"),
            Seed(9, "project", 1, "Всего проектов", null),
            Seed(10, "project", 2, "Размещено ссылок", null),
            Seed(11, "project", 3, "Ссылки в работе", null),
            Seed(12, "project", 4, "Потрачено", "₽"));
    }
}
