using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfSiteReviewRepository(ApplicationDbContext db) : ISiteReviewRepository
{
    public Task<bool> ExistsForPurchasedSiteAsync(int purchasedSiteId, CancellationToken cancellationToken = default) =>
        db.SiteReviews.AnyAsync(r => r.PurchasedSiteId == purchasedSiteId, cancellationToken);

    public async Task AddAsync(SiteReview review, CancellationToken cancellationToken = default) =>
        await db.SiteReviews.AddAsync(review, cancellationToken);

    public Task<SiteReview?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.SiteReviews.Include(r => r.Site).Include(r => r.Buyer).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<SiteReview> Items, int Total)> GetBySiteAsync(int siteId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.SiteReviews
            .AsNoTracking()
            .Include(r => r.Buyer)
            .Where(r => r.SiteId == siteId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(decimal? AverageRating, int Count)> GetSummaryBySellerAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        var ratings = await db.SiteReviews.Where(r => r.Site.SellerId == sellerId).Select(r => r.Rating).ToListAsync(cancellationToken);
        return ratings.Count == 0 ? (null, 0) : (Math.Round((decimal)ratings.Average(), 1), ratings.Count);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
