using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface ITrustedDeviceRepository
{
    Task AddAsync(TrustedDevice device, CancellationToken cancellationToken = default);

    Task<TrustedDevice?> GetValidByHashAsync(int userId, string tokenHash, CancellationToken cancellationToken = default);

    // Newest first — backs the profile page's "Доверенные устройства" list.
    Task<IReadOnlyList<TrustedDevice>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<TrustedDevice?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task DeleteAsync(TrustedDevice device, CancellationToken cancellationToken = default);

    // Called whenever 2FA itself is disabled — a trusted-device bypass only makes sense while
    // 2FA is on.
    Task DeleteAllForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
