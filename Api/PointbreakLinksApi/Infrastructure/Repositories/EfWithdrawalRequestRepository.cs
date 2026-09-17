using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfWithdrawalRequestRepository(ApplicationDbContext db) : IWithdrawalRequestRepository
{
    public async Task AddAsync(WithdrawalRequest request, CancellationToken cancellationToken = default) =>
        await db.WithdrawalRequests.AddAsync(request, cancellationToken);

    public Task<WithdrawalRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.WithdrawalRequests.Include(w => w.User).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<WithdrawalRequest> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.WithdrawalRequests.Where(w => w.UserId == userId).OrderByDescending(w => w.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<WithdrawalRequest> Items, int Total)> GetPendingAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.WithdrawalRequests.Include(w => w.User)
            .Where(w => w.Status == WithdrawalRequestStatus.Pending)
            .OrderBy(w => w.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<WithdrawalExportRow>> GetAllForExportAsync(CancellationToken cancellationToken = default) =>
        await db.WithdrawalRequests
            .Include(w => w.User)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WithdrawalExportRow(
                w.Id,
                w.User.Name,
                w.User.Email,
                w.Amount,
                w.PayoutDetails,
                w.Status.ToString(),
                w.CreatedAt,
                w.ProcessedAt,
                w.AdminComment))
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
