using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfBalanceTransactionRepository(ApplicationDbContext db) : IBalanceTransactionRepository
{
    public async Task AddAsync(BalanceTransaction transaction, CancellationToken cancellationToken = default) =>
        await db.BalanceTransactions.AddAsync(transaction, cancellationToken);

    public async Task<(IReadOnlyList<BalanceTransaction> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.BalanceTransactions.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<TransactionExportRow>> GetAllForExportAsync(CancellationToken cancellationToken = default) =>
        await db.BalanceTransactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionExportRow(t.Id, t.User.Name, t.Type.ToString(), t.Amount, t.Description, t.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<TransactionExportRow> Items, int Total)> GetAllPaginatedAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.BalanceTransactions.OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(t => new TransactionExportRow(t.Id, t.User.Name, t.Type.ToString(), t.Amount, t.Description, t.CreatedAt))
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
