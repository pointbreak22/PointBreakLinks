using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class LoginHistoryEntryConfiguration : IEntityTypeConfiguration<LoginHistoryEntry>
{
    public void Configure(EntityTypeBuilder<LoginHistoryEntry> builder)
    {
        builder.ToTable("login_history");
        builder.Property(e => e.UserAgent).HasMaxLength(300);
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
