using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("sites");
        builder.HasIndex(s => s.Url).IsUnique();
        builder.Property(s => s.Url).IsRequired();
        builder.Property(s => s.Price).HasPrecision(10, 2);
        builder.Property(s => s.VerificationToken).IsRequired().HasMaxLength(64).HasDefaultValue(string.Empty);
        builder.Property(s => s.IsVerified).HasDefaultValue(false);

        builder.HasOne(s => s.Country)
            .WithMany(c => c.Sites)
            .HasForeignKey(s => s.CountryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Topic)
            .WithMany(t => t.Sites)
            .HasForeignKey(s => s.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Status)
            .WithMany()
            .HasForeignKey(s => s.StatusId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Seller)
            .WithMany()
            .HasForeignKey(s => s.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
