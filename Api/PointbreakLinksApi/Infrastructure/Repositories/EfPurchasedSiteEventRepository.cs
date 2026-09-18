using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfPurchasedSiteEventRepository(ApplicationDbContext db) : IPurchasedSiteEventRepository
{
    public async Task AddAsync(PurchasedSiteEvent purchasedSiteEvent, CancellationToken cancellationToken = default) =>
        await db.PurchasedSiteEvents.AddAsync(purchasedSiteEvent, cancellationToken);

    public async Task<IReadOnlyList<PurchasedSiteEvent>> GetByPurchasedSiteAsync(int purchasedSiteId, CancellationToken cancellationToken = default) =>
        await db.PurchasedSiteEvents
            .AsNoTracking()
            .Where(e => e.PurchasedSiteId == purchasedSiteId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<(IReadOnlyList<SystemLogRow> Items, int Total)> GetAllForAdminAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.PurchasedSiteEvents.AsNoTracking().OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(e => new SystemLogRow(e.Id, e.PurchasedSiteId, e.PurchasedSite.Site.Url, e.Description, e.CreatedAt))
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
