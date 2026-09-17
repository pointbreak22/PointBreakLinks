using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfDynamicStatRepository(ApplicationDbContext db) : IDynamicStatRepository
{
    public Task<List<DynamicStat>> GetByPageKeyAsync(string pageKey, CancellationToken cancellationToken = default) =>
        db.DynamicStats.Where(s => s.PageKey == pageKey).OrderBy(s => s.Position).ToListAsync(cancellationToken);
}
