using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class TwoFactorLoginTicketConfiguration : IEntityTypeConfiguration<TwoFactorLoginTicket>
{
    public void Configure(EntityTypeBuilder<TwoFactorLoginTicket> builder)
    {
        builder.ToTable("two_factor_login_tickets");
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.Property(t => t.TokenHash).IsRequired();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
