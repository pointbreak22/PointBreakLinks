using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SiteReviewConfiguration : IEntityTypeConfiguration<SiteReview>
{
    public void Configure(EntityTypeBuilder<SiteReview> builder)
    {
        builder.ToTable("site_reviews");
        builder.HasIndex(r => r.SiteId);
        // One review per order — enforced here, not just in the command handler.
        builder.HasIndex(r => r.PurchasedSiteId).IsUnique();

        builder.HasOne(r => r.Site)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Buyer)
            .WithMany()
            .HasForeignKey(r => r.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.PurchasedSite)
            .WithOne(ps => ps.Review)
            .HasForeignKey<SiteReview>(r => r.PurchasedSiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
