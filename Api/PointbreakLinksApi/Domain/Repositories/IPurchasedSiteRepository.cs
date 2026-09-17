using Domain.Entities;

namespace Domain.Repositories;

// Admin reports export row — a flat projection, not the full entity graph IncludeAll() loads
// (Reviews/Links/Messages aren't needed for a CSV line), and includes the seller's name, which
// none of the existing DTOs carry (see ConversationDto's comment for the same gap).
public record OrderExportRow(int Id, string SiteUrl, string BuyerName, string SellerName, decimal FinalPrice, string Status, DateTime CreatedAt);

// Admin dispute-queue row — same flat-projection reasoning as OrderExportRow.
public record DisputedOrderRow(int Id, string SiteUrl, string BuyerName, string SellerName, decimal FinalPrice, string Reason, DateTime UpdatedAt);

public interface IPurchasedSiteRepository
{
    // Admin-only, system-wide, no ownership filter — authorization is enforced by the
    // controller's [Authorize(Roles = Admin)], not here.
    Task<IReadOnlyList<OrderExportRow>> GetAllForExportAsync(CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PurchasedSite> Items, int Total)> GetSalesByWebmasterAsync(int webmasterId, int page, int perPage, CancellationToken cancellationToken = default);

    // Project ownership is checked by the query handler (via IProjectRepository) before this is
    // called — FOXLinks' getByProjectQuery() filters only by project_id, with no ownership
    // check at all, letting any authenticated user read any project's purchased sites by
    // guessing its id; not replicated.
    Task<(IReadOnlyList<PurchasedSite> Items, int Total)> GetByProjectAsync(int projectId, int page, int perPage, CancellationToken cancellationToken = default);

    // Ownership check (order belongs to one of the seller's Sites) done at the query level,
    // same as FOXLinks' acceptOrder()/confirmPublished() — a seller can't act on someone
    // else's order just by guessing an id.
    Task<PurchasedSite?> GetByIdForSellerAsync(int purchasedSiteId, int sellerId, CancellationToken cancellationToken = default);

    // Same ownership-at-the-query-level pattern, for the buyer side — used when a buyer acts on
    // their own order (e.g. leaving a review, Application/CQRS/Reviews).
    Task<PurchasedSite?> GetByIdForBuyerAsync(int purchasedSiteId, int buyerId, CancellationToken cancellationToken = default);

    // Re-fetch after AddAsync+SaveChangesAsync, to load navigations for the response DTO —
    // ownership is already established by who set BuyerId when the order was created.
    Task<PurchasedSite?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(PurchasedSite purchasedSite, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Admin-only, system-wide — every order currently flagged IsDisputed.
    Task<(IReadOnlyList<DisputedOrderRow> Items, int Total)> GetDisputedAsync(int page, int perPage, CancellationToken cancellationToken = default);
}
