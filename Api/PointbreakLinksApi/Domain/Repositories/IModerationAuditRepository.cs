using Domain.Entities;

namespace Domain.Repositories;

public interface IModerationAuditRepository
{
    Task AddAsync(ModerationAuditEntry entry, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ModerationAuditEntry> Items, int Total)> GetPagedAsync(
        int page, int perPage, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
