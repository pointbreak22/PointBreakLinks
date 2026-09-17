using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface ILoginHistoryRepository
{
    Task AddAsync(LoginHistoryEntry entry, CancellationToken cancellationToken = default);

    // Newest first — backs the profile page's "История входов" list.
    Task<IReadOnlyList<LoginHistoryEntry>> GetByUserAsync(int userId, int limit, CancellationToken cancellationToken = default);

    // Backs NewDeviceLoginNotifier — must be called before this login's own entry is added,
    // otherwise it would always find itself and never alert.
    Task<bool> ExistsForDeviceAsync(int userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
