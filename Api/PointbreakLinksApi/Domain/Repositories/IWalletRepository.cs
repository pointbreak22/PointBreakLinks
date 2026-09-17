using Domain.Entities;

namespace Domain.Repositories;

public interface IWalletRepository
{
    // Creates a zero-balance row on first access — see Wallet's comment for why this is lazy
    // rather than created at registration time.
    Task<Wallet> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
