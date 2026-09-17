using Domain.Entities;

namespace Domain.Repositories;

public interface ISavedSearchRepository
{
    Task AddAsync(SavedSearch search, CancellationToken cancellationToken = default);

    // Requires Topic/Country loaded — callers read the display names directly.
    Task<IReadOnlyList<SavedSearch>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<SavedSearch?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task DeleteAsync(SavedSearch search, CancellationToken cancellationToken = default);

    // Every saved search whose criteria this site currently satisfies, excluding the site's own
    // seller — used by ApproveSiteCommandHandler the moment a listing first enters the catalog.
    Task<IReadOnlyList<SavedSearch>> GetMatchingAsync(Site site, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
