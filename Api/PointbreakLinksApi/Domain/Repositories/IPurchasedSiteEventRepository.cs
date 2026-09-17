using Domain.Entities;

namespace Domain.Repositories;

// Admin "system logs" row — every PurchasedSiteEvent platform-wide, with enough context (which
// site) to be readable outside of one order's own timeline view.
public record SystemLogRow(int Id, int PurchasedSiteId, string SiteUrl, string Description, DateTime CreatedAt);

public interface IPurchasedSiteEventRepository
{
    Task AddAsync(PurchasedSiteEvent purchasedSiteEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchasedSiteEvent>> GetByPurchasedSiteAsync(int purchasedSiteId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Admin-only, system-wide — see OrderExportRow's authorization note (Domain/Repositories/
    // IPurchasedSiteRepository.cs).
    Task<(IReadOnlyList<SystemLogRow> Items, int Total)> GetAllForAdminAsync(int page, int perPage, CancellationToken cancellationToken = default);
}
