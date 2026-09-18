using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfFavoriteSiteRepository(ApplicationDbContext db) : IFavoriteSiteRepository
{
    public Task<bool> ExistsAsync(int buyerId, int siteId, CancellationToken cancellationToken = default) =>
        db.FavoriteSites.AnyAsync(f => f.BuyerId == buyerId && f.SiteId == siteId, cancellationToken);

    public async Task AddAsync(FavoriteSite favorite, CancellationToken cancellationToken = default) =>
        await db.FavoriteSites.AddAsync(favorite, cancellationToken);

    public async Task RemoveAsync(int buyerId, int siteId, CancellationToken cancellationToken = default)
    {
        var favorite = await db.FavoriteSites.FirstOrDefaultAsync(f => f.BuyerId == buyerId && f.SiteId == siteId, cancellationToken);
        if (favorite != null)
        {
            db.FavoriteSites.Remove(favorite);
        }
    }

    public async Task<(IReadOnlyList<Site> Items, int Total)> GetFavoriteSitesAsync(int buyerId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        // Include on FavoriteSite, not a post-Select() Site query — EF Core can't apply
        // Include() after a projection.
        var query = db.FavoriteSites
            .AsNoTracking()
            .Where(f => f.BuyerId == buyerId)
            .Include(f => f.Site).ThenInclude(s => s.Topic)
            .Include(f => f.Site).ThenInclude(s => s.Status)
            .Include(f => f.Site).ThenInclude(s => s.Country)
            .OrderByDescending(f => f.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var favorites = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (favorites.Select(f => f.Site).ToList(), total);
    }

    public async Task<IReadOnlyList<int>> GetFavoriteSiteIdsAsync(int buyerId, CancellationToken cancellationToken = default) =>
        await db.FavoriteSites.Where(f => f.BuyerId == buyerId).Select(f => f.SiteId).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
