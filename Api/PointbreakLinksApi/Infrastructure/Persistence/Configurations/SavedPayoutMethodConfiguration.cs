using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SavedPayoutMethodConfiguration : IEntityTypeConfiguration<SavedPayoutMethod>
{
    public void Configure(EntityTypeBuilder<SavedPayoutMethod> builder)
    {
        builder.ToTable("saved_payout_methods");
        builder.Property(m => m.Label).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Details).IsRequired().HasMaxLength(300);
        builder.HasIndex(m => m.UserId);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
