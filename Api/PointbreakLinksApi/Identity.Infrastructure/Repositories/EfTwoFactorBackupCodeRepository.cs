using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class EfTwoFactorBackupCodeRepository(IdentityDbContext db) : ITwoFactorBackupCodeRepository
{
    public async Task AddRangeAsync(IEnumerable<TwoFactorBackupCode> codes, CancellationToken cancellationToken = default) =>
        await db.TwoFactorBackupCodes.AddRangeAsync(codes, cancellationToken);

    public Task<TwoFactorBackupCode?> GetUnusedByHashAsync(int userId, string codeHash, CancellationToken cancellationToken = default) =>
        db.TwoFactorBackupCodes.FirstOrDefaultAsync(c => c.UserId == userId && c.CodeHash == codeHash && !c.IsUsed, cancellationToken);

    public Task<int> CountUnusedAsync(int userId, CancellationToken cancellationToken = default) =>
        db.TwoFactorBackupCodes.CountAsync(c => c.UserId == userId && !c.IsUsed, cancellationToken);

    public async Task DeleteAllForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var codes = await db.TwoFactorBackupCodes.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
        db.TwoFactorBackupCodes.RemoveRange(codes);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
