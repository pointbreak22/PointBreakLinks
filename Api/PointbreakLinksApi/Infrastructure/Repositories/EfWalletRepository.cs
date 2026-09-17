using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfWalletRepository(ApplicationDbContext db) : IWalletRepository
{
    public async Task<Wallet> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet != null)
        {
            return wallet;
        }

        wallet = new Wallet { UserId = userId, Balance = 0m };
        await db.Wallets.AddAsync(wallet, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
