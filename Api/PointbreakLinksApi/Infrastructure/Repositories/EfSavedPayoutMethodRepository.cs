using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfSavedPayoutMethodRepository(ApplicationDbContext db) : ISavedPayoutMethodRepository
{
    public async Task<IReadOnlyList<SavedPayoutMethod>> GetByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.SavedPayoutMethods.AsNoTracking().Where(m => m.UserId == userId).OrderByDescending(m => m.CreatedAt).ToListAsync(cancellationToken);

    public Task<SavedPayoutMethod?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default) =>
        db.SavedPayoutMethods.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, cancellationToken);

    public async Task AddAsync(SavedPayoutMethod method, CancellationToken cancellationToken = default) =>
        await db.SavedPayoutMethods.AddAsync(method, cancellationToken);

    public Task DeleteAsync(SavedPayoutMethod method, CancellationToken cancellationToken = default)
    {
        db.SavedPayoutMethods.Remove(method);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
