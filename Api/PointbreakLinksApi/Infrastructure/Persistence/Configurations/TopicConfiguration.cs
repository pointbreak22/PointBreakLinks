using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("topics");
        builder.HasIndex(t => t.Name).IsUnique();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(255);

        // Seeded 1:1 with FOXLinks' TopicSeeder.
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        string[] names =
        [
            "Авто и Мото", "Транспортные услуги", "Бизнес и Финансы", "Офис", "Бытовая техника",
            "Недвижимость", "Здоровье и медицина", "Интернет и маркетинг", "Строительство и ремонт",
            "Сельское хозяйство и производство", "Работа", "Мебель и быт", "Туризм и путешествия",
            "Компьютеры и компьютерные игры", "Женский раздел", "Спорт", "Отдых и развлечения",
            "Культура и искусство", "Образование и наука", "СМИ и порталы", "Иноязычные",
            "Мобильные технологии", "Фотография и фотоуслуги", "Товары и Услуги",
        ];
        builder.HasData(names.Select((name, index) => new Topic
        {
            Id = index + 1,
            Name = name,
            CreatedAt = seededAt,
            UpdatedAt = seededAt,
        }));
    }
}
