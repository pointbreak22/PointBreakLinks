using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    // Mirrors FOXLinks' RefreshToken::updateOrCreate(['user_id' => ...]): one active
    // refresh token per user, so signing in elsewhere invalidates the previous one.
    Task UpsertForUserAsync(int userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default);

    Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task DeleteForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
