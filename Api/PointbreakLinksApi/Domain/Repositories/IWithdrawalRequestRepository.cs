using Domain.Entities;

namespace Domain.Repositories;

// Admin reports export row — see OrderExportRow's comment in IPurchasedSiteRepository for why
// this is a flat projection rather than the full entity.
public record WithdrawalExportRow(
    int Id,
    string UserName,
    string UserEmail,
    decimal Amount,
    string PayoutDetails,
    string Status,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    string? AdminComment);

public interface IWithdrawalRequestRepository
{
    Task AddAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);

    // Requires User loaded — callers read User.Email/Name directly (approve/reject emails).
    Task<WithdrawalRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<WithdrawalRequest> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default);

    // Admin queue — pending only, same "disappears once handled" shape as
    // IPurchasedSiteRepository.GetDisputedAsync.
    Task<(IReadOnlyList<WithdrawalRequest> Items, int Total)> GetPendingAsync(int page, int perPage, CancellationToken cancellationToken = default);

    // Admin-only, system-wide, every status — see OrderExportRow's authorization note.
    Task<IReadOnlyList<WithdrawalExportRow>> GetAllForExportAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
