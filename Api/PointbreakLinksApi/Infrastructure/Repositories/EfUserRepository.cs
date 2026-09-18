using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfUserRepository(ApplicationDbContext db) : IUserRepository
{
    // NOT AsNoTracking: RegisterCommandHandler attaches the returned Role to a new User's
    // Roles collection — an untracked reference here would make EF treat it as a new row to
    // insert instead of an existing one to link, duplicating/corrupting the roles table.
    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int Total)> GetAllPaginatedAsync(
        int page, int perPage, string? search = null, string? role = null, CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking().Include(u => u.Roles).Include(u => u.Projects).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Name, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Roles.Any(r => r.Name == role));
        }

        query = query.OrderByDescending(u => u.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<User?> GetByIdWithRolesAndProjectsAsync(int id, CancellationToken cancellationToken = default) =>
        db.Users.Include(u => u.Roles).Include(u => u.Projects).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
