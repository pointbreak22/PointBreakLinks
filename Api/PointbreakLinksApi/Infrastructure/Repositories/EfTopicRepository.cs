using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfTopicRepository(ApplicationDbContext db) : ITopicRepository
{
    public Task<List<Topic>> GetAllAsync(CancellationToken cancellationToken = default) =>
        db.Topics.OrderBy(t => t.Name).ToListAsync(cancellationToken);
}
