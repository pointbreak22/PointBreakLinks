using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfSavedSearchRepository(ApplicationDbContext db) : ISavedSearchRepository
{
    public async Task AddAsync(SavedSearch search, CancellationToken cancellationToken = default) =>
        await db.SavedSearches.AddAsync(search, cancellationToken);

    public async Task<IReadOnlyList<SavedSearch>> GetByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.SavedSearches
            .AsNoTracking()
            .Include(s => s.Topic)
            .Include(s => s.Country)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<SavedSearch?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default) =>
        db.SavedSearches
            .Include(s => s.Topic)
            .Include(s => s.Country)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);

    public Task DeleteAsync(SavedSearch search, CancellationToken cancellationToken = default)
    {
        db.SavedSearches.Remove(search);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<SavedSearch>> GetMatchingAsync(Site site, CancellationToken cancellationToken = default) =>
        await db.SavedSearches
            .AsNoTracking()
            .Where(s => s.UserId != site.SellerId)
            .Where(s => s.TopicId == null || s.TopicId == site.TopicId)
            .Where(s => s.CountryId == null || s.CountryId == site.CountryId)
            .Where(s => s.MinPrice == null || site.Price >= s.MinPrice)
            .Where(s => s.MaxPrice == null || site.Price <= s.MaxPrice)
            .Where(s => s.MinIks == null || site.Iks >= s.MinIks)
            .Where(s => s.MinDr == null || site.Dr >= s.MinDr)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
