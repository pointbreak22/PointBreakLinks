using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PurchasedSiteConfiguration : IEntityTypeConfiguration<PurchasedSite>
{
    public void Configure(EntityTypeBuilder<PurchasedSite> builder)
    {
        builder.ToTable("purchased_sites");
        builder.Property(ps => ps.FinalPrice).HasPrecision(10, 2);
        builder.HasIndex(ps => ps.IsPublicationRequested);
        builder.HasIndex(ps => ps.IsPublished);

        builder.HasOne(ps => ps.Project)
            .WithMany(p => p.PurchasedSites)
            .HasForeignKey(ps => ps.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Site)
            .WithMany(s => s.PurchasedSites)
            .HasForeignKey(ps => ps.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Buyer)
            .WithMany()
            .HasForeignKey(ps => ps.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Status)
            .WithMany()
            .HasForeignKey(ps => ps.StatusId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.PaymentSetting)
            .WithMany()
            .HasForeignKey(ps => ps.PaymentSettingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
