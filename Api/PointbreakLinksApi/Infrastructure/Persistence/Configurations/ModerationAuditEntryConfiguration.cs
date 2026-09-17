using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ModerationAuditEntryConfiguration : IEntityTypeConfiguration<ModerationAuditEntry>
{
    public void Configure(EntityTypeBuilder<ModerationAuditEntry> builder)
    {
        builder.ToTable("moderation_audit_entries");
        builder.Property(e => e.Action).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.HasIndex(e => e.CreatedAt);

        builder.HasOne(e => e.Site)
            .WithMany()
            .HasForeignKey(e => e.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Moderator)
            .WithMany()
            .HasForeignKey(e => e.ModeratorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
