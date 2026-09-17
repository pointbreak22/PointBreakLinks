using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class BalanceTransactionConfiguration : IEntityTypeConfiguration<BalanceTransaction>
{
    public void Configure(EntityTypeBuilder<BalanceTransaction> builder)
    {
        builder.ToTable("balance_transactions");
        builder.HasIndex(t => t.UserId);
        builder.Property(t => t.Amount).HasPrecision(10, 2);
        builder.Property(t => t.Description).IsRequired();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.PurchasedSite)
            .WithMany()
            .HasForeignKey(t => t.PurchasedSiteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
