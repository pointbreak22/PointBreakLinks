using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface ITwoFactorBackupCodeRepository
{
    Task AddRangeAsync(IEnumerable<TwoFactorBackupCode> codes, CancellationToken cancellationToken = default);

    Task<TwoFactorBackupCode?> GetUnusedByHashAsync(int userId, string codeHash, CancellationToken cancellationToken = default);

    Task<int> CountUnusedAsync(int userId, CancellationToken cancellationToken = default);

    // Called both when 2FA is disabled outright (old codes become meaningless) and right before
    // minting a fresh batch on regenerate (no point keeping unused-but-orphaned rows around).
    Task DeleteAllForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
