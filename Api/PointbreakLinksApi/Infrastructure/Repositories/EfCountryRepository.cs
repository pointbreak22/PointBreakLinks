using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfCountryRepository(ApplicationDbContext db) : ICountryRepository
{
    public Task<List<Country>> GetAllAsync(CancellationToken cancellationToken = default) =>
        db.Countries.OrderBy(c => c.Name).ToListAsync(cancellationToken);
}
