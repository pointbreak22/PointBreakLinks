using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfProjectRepository(ApplicationDbContext db) : IProjectRepository
{
    public async Task<(IReadOnlyList<Project> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.Projects.AsNoTracking().Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<AdminProjectRow> Items, int Total)> GetAllForAdminAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.Projects.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(p => new AdminProjectRow(p.Id, p.Name, p.User.Name, p.Type, p.TotalLinks, p.LinksPosted, p.SpentMoney, p.CreatedAt))
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Project?> GetByIdForOwnerAsync(int id, int ownerId, CancellationToken cancellationToken = default) =>
        db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == ownerId, cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default) =>
        await db.Projects.AddAsync(project, cancellationToken);

    public void Remove(Project project) => db.Projects.Remove(project);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
