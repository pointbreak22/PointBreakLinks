using Domain.Entities;

namespace Domain.Repositories;

// Admin projects report row — see OrderExportRow's comment in IPurchasedSiteRepository for why
// this is a flat projection (needs the owner's name, which Project itself doesn't carry).
public record AdminProjectRow(int Id, string Name, string OwnerName, string Type, int TotalLinks, int LinksPosted, long SpentMoney, DateTime CreatedAt);

public interface IProjectRepository
{
    Task<(IReadOnlyList<Project> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default);

    // Admin-only, system-wide — see OrderExportRow's authorization note.
    Task<(IReadOnlyList<AdminProjectRow> Items, int Total)> GetAllForAdminAsync(int page, int perPage, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdForOwnerAsync(int id, int ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    void Remove(Project project);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
