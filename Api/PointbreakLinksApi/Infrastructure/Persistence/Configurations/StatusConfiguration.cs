using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("statuses");
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);

        // Seeded 1:1 with FOXLinks' StatusSeeder. IDs are relied upon by StoreSiteRequest's
        // hardcoded "status_id: 2" (moderation) equivalent in CreateSiteCommandHandler.
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Status { Id = 1, Name = StatusNames.Application, Description = "Заявка", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Status { Id = 2, Name = StatusNames.Moderation, Description = "На модерации", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Status { Id = 3, Name = StatusNames.Rejected, Description = "Отклонена", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Status { Id = 4, Name = StatusNames.Work, Description = "в работе", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Status { Id = 5, Name = StatusNames.Paid, Description = "Оплачена", CreatedAt = seededAt, UpdatedAt = seededAt },
            // Added this session for the moderation queue — see StatusNames.Active's comment.
            new Status { Id = 6, Name = StatusNames.Active, Description = "Активна", CreatedAt = seededAt, UpdatedAt = seededAt },
            // Added this session for order cancel/decline — see StatusNames.Cancelled's comment.
            new Status { Id = 7, Name = StatusNames.Cancelled, Description = "Отменена", CreatedAt = seededAt, UpdatedAt = seededAt }
        );
    }
}
