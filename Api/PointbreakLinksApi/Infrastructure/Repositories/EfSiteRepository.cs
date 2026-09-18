using Domain.Constants;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfSiteRepository(ApplicationDbContext db) : ISiteRepository
{
    private IQueryable<Site> IncludeAll() =>
        db.Sites.Include(s => s.Topic).Include(s => s.Status).Include(s => s.Country).Include(s => s.Reviews).Include(s => s.Seller);

    public async Task<(IReadOnlyList<Site> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = IncludeAll()
            .AsNoTracking()
            .Where(s => s.SellerId == userId)
            .OrderByDescending(s => s.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Site>> GetActiveByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await IncludeAll()
            .AsNoTracking()
            .Where(s => s.SellerId == userId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Site> Items, int Total)> GetCatalogAsync(int page, int perPage, SiteCatalogFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = IncludeAll().AsNoTracking().Where(s => s.IsActive);

        if (filter != null)
        {
            if (filter.TopicId.HasValue) query = query.Where(s => s.TopicId == filter.TopicId.Value);
            if (filter.CountryId.HasValue) query = query.Where(s => s.CountryId == filter.CountryId.Value);
            if (filter.MinPrice.HasValue) query = query.Where(s => s.Price >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue) query = query.Where(s => s.Price <= filter.MaxPrice.Value);
            if (filter.MinIks.HasValue) query = query.Where(s => s.Iks >= filter.MinIks.Value);
            if (filter.MinDr.HasValue) query = query.Where(s => s.Dr >= filter.MinDr.Value);
        }

        query = (filter?.SortBy, filter?.SortDescending) switch
        {
            ("price", true) => query.OrderByDescending(s => s.Price),
            ("price", false) => query.OrderBy(s => s.Price),
            ("iks", true) => query.OrderByDescending(s => s.Iks),
            ("iks", false) => query.OrderBy(s => s.Iks),
            ("dr", true) => query.OrderByDescending(s => s.Dr),
            ("dr", false) => query.OrderBy(s => s.Dr),
            ("traffic", true) => query.OrderByDescending(s => s.Traffic),
            ("traffic", false) => query.OrderBy(s => s.Traffic),
            _ => query.OrderByDescending(s => s.CreatedAt),
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    // NOT AsNoTracking: called by Approve/Reject/UpdateSiteCommandHandler, which mutate the
    // returned site and call SaveChangesAsync.
    public Task<Site?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        IncludeAll().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    // NOT AsNoTracking: same reasoning — Deactivate/Reactivate/Update/VerifySiteCommandHandler
    // all mutate the returned, ownership-checked site.
    public Task<Site?> GetByIdForOwnerAsync(int id, int ownerId, CancellationToken cancellationToken = default) =>
        IncludeAll().FirstOrDefaultAsync(s => s.Id == id && s.SellerId == ownerId, cancellationToken);

    public async Task<(IReadOnlyList<Site> Items, int Total)> GetPendingModerationAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.Sites
            .AsNoTracking()
            .Include(s => s.Topic)
            .Include(s => s.Status)
            .Include(s => s.Country)
            .Include(s => s.Seller)
            .Where(s => s.Status.Name == StatusNames.Moderation)
            .OrderBy(s => s.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Site>> GetAllVerifiedAsync(CancellationToken cancellationToken = default) =>
        await db.Sites.Where(s => s.IsVerified).ToListAsync(cancellationToken);

    public Task<bool> UrlExistsAsync(string url, int? excludeSiteId = null, CancellationToken cancellationToken = default) =>
        db.Sites.AnyAsync(s => s.Url == url && s.Id != (excludeSiteId ?? -1), cancellationToken);

    public async Task AddAsync(Site site, CancellationToken cancellationToken = default) =>
        await db.Sites.AddAsync(site, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
