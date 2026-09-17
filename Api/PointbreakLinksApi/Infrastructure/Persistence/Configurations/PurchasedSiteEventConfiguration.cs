using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PurchasedSiteEventConfiguration : IEntityTypeConfiguration<PurchasedSiteEvent>
{
    public void Configure(EntityTypeBuilder<PurchasedSiteEvent> builder)
    {
        builder.ToTable("purchased_site_events");
        builder.HasIndex(e => e.PurchasedSiteId);
        builder.Property(e => e.Description).IsRequired();

        builder.HasOne(e => e.PurchasedSite)
            .WithMany()
            .HasForeignKey(e => e.PurchasedSiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
