using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class FavoriteSiteConfiguration : IEntityTypeConfiguration<FavoriteSite>
{
    public void Configure(EntityTypeBuilder<FavoriteSite> builder)
    {
        builder.ToTable("favorite_sites");
        builder.HasIndex(f => new { f.BuyerId, f.SiteId }).IsUnique();

        builder.HasOne(f => f.Buyer)
            .WithMany()
            .HasForeignKey(f => f.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Site)
            .WithMany()
            .HasForeignKey(f => f.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
