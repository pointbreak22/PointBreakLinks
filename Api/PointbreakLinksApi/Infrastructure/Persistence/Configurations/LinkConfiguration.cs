using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class LinkConfiguration : IEntityTypeConfiguration<Link>
{
    public void Configure(EntityTypeBuilder<Link> builder)
    {
        builder.ToTable("links");
        builder.Property(l => l.Url).IsRequired();
        builder.Property(l => l.Name).IsRequired();

        builder.HasOne(l => l.PurchasedSite)
            .WithMany(ps => ps.Links)
            .HasForeignKey(l => l.PurchasedSiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
