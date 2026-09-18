using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfModerationAuditRepository(ApplicationDbContext db) : IModerationAuditRepository
{
    public async Task AddAsync(ModerationAuditEntry entry, CancellationToken cancellationToken = default) =>
        await db.ModerationAuditEntries.AddAsync(entry, cancellationToken);

    public async Task<(IReadOnlyList<ModerationAuditEntry> Items, int Total)> GetPagedAsync(
        int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.ModerationAuditEntries
            .AsNoTracking()
            .Include(e => e.Site)
            .Include(e => e.Moderator)
            .OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
