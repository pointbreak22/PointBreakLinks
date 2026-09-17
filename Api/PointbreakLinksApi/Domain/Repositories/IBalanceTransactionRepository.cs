using Domain.Entities;

namespace Domain.Repositories;

// Admin reports export row — see OrderExportRow's comment in IPurchasedSiteRepository for why
// this is a flat projection rather than the full entity.
public record TransactionExportRow(int Id, string UserName, string Type, decimal Amount, string Description, DateTime CreatedAt);

public interface IBalanceTransactionRepository
{
    Task AddAsync(BalanceTransaction transaction, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<BalanceTransaction> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Admin-only, system-wide — see OrderExportRow's authorization note.
    Task<IReadOnlyList<TransactionExportRow>> GetAllForExportAsync(CancellationToken cancellationToken = default);

    // Same data as GetAllForExportAsync, paginated — backs the admin "Транзакции" table page,
    // where loading every row at once (fine for a CSV download) isn't appropriate.
    Task<(IReadOnlyList<TransactionExportRow> Items, int Total)> GetAllPaginatedAsync(int page, int perPage, CancellationToken cancellationToken = default);
}
