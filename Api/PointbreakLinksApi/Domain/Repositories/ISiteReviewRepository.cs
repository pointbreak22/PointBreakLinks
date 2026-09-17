using Domain.Entities;

namespace Domain.Repositories;

public interface ISiteReviewRepository
{
    Task<bool> ExistsForPurchasedSiteAsync(int purchasedSiteId, CancellationToken cancellationToken = default);
    Task AddAsync(SiteReview review, CancellationToken cancellationToken = default);

    // Requires Site and Buyer loaded — backs ReplyToReviewCommandHandler's ownership check and
    // its returned DTO.
    Task<SiteReview?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SiteReview> Items, int Total)> GetBySiteAsync(int siteId, int page, int perPage, CancellationToken cancellationToken = default);

    // Aggregated across every site the seller owns — the public seller-profile page's headline
    // rating, not scoped to any one listing the way GetBySiteAsync is.
    Task<(decimal? AverageRating, int Count)> GetSummaryBySellerAsync(int sellerId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
