using Domain.Entities;

namespace Domain.Repositories;

public interface ISavedPayoutMethodRepository
{
    // Newest first — backs the wallet page's dropdown.
    Task<IReadOnlyList<SavedPayoutMethod>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<SavedPayoutMethod?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task AddAsync(SavedPayoutMethod method, CancellationToken cancellationToken = default);

    Task DeleteAsync(SavedPayoutMethod method, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
