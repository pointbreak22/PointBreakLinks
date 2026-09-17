using Domain.Entities;

namespace Domain.Repositories;

public interface IFavoriteSiteRepository
{
    Task<bool> ExistsAsync(int buyerId, int siteId, CancellationToken cancellationToken = default);
    Task AddAsync(FavoriteSite favorite, CancellationToken cancellationToken = default);

    // No-op (doesn't throw) if the favorite doesn't exist — removing something already gone is
    // not an error from the caller's point of view.
    Task RemoveAsync(int buyerId, int siteId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Site> Items, int Total)> GetFavoriteSitesAsync(int buyerId, int page, int perPage, CancellationToken cancellationToken = default);

    // Cheap overlay for the catalog table — which of the currently-visible sites are already
    // favorited, without joining favorites into GetCatalogAsync's per-viewer-agnostic query.
    Task<IReadOnlyList<int>> GetFavoriteSiteIdsAsync(int buyerId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
