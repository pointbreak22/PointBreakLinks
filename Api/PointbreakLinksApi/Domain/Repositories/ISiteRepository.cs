using Domain.Entities;

namespace Domain.Repositories;

// New, not from FOXLinks — its "Расширенный поиск"/"Фильтрация" panels on optimizator.vue were
// entirely decorative (see pages/_NEXT.md), so this is a real filter set built from scratch
// rather than a port of anything.
public record SiteCatalogFilter(
    int? TopicId = null,
    int? CountryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinIks = null,
    int? MinDr = null,
    // One of "price"/"iks"/"dr"/"traffic" — anything else (including null) keeps the default
    // CreatedAt-descending order. See EfSiteRepository.GetCatalogAsync for the switch.
    string? SortBy = null,
    bool SortDescending = false);

public interface ISiteRepository
{
    // Paging happens inside the Infrastructure implementation (EF Core's Skip/Take + Count),
    // not exposed as IQueryable here — keeps Application free of an EF Core reference, same
    // split MessengerAzure's repositories use.
    Task<(IReadOnlyList<Site> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default);

    // All active listings, any seller — the public catalog (FOXLinks' SiteController::index /
    // getAllSitesPaginated). No ownership filter by design.
    Task<(IReadOnlyList<Site> Items, int Total)> GetCatalogAsync(int page, int perPage, SiteCatalogFilter? filter = null, CancellationToken cancellationToken = default);

    Task<Site?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Site?> GetByIdForOwnerAsync(int id, int ownerId, CancellationToken cancellationToken = default);

    // Public seller-profile page — active listings only, unpaginated (a seller's public
    // storefront, not expected to run into thousands of rows the way the full catalog might).
    Task<IReadOnlyList<Site>> GetActiveByUserAsync(int userId, CancellationToken cancellationToken = default);

    // Moderation queue — sites awaiting review (Application/CQRS/Moderation).
    Task<(IReadOnlyList<Site> Items, int Total)> GetPendingModerationAsync(int page, int perPage, CancellationToken cancellationToken = default);

    // Scheduled ownership recheck (Infrastructure/BackgroundJobs/SiteReverificationJob) — every
    // currently-verified site, unpaginated since this only ever runs on a timer against the
    // whole table, not from a user-facing request.
    Task<IReadOnlyList<Site>> GetAllVerifiedAsync(CancellationToken cancellationToken = default);

    // excludeSiteId lets UpdateSiteCommandHandler check "does another site have this URL"
    // without tripping over the site's own current URL.
    Task<bool> UrlExistsAsync(string url, int? excludeSiteId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Site site, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
