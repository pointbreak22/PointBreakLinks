using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class EfLoginHistoryRepository(IdentityDbContext db) : ILoginHistoryRepository
{
    public async Task AddAsync(LoginHistoryEntry entry, CancellationToken cancellationToken = default) =>
        await db.LoginHistory.AddAsync(entry, cancellationToken);

    public async Task<IReadOnlyList<LoginHistoryEntry>> GetByUserAsync(int userId, int limit, CancellationToken cancellationToken = default) =>
        await db.LoginHistory
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForDeviceAsync(int userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default) =>
        db.LoginHistory.AnyAsync(e => e.UserId == userId && e.IpAddress == ipAddress && e.UserAgent == userAgent, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
