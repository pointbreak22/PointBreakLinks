using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfSupportTicketRepository(ApplicationDbContext db) : ISupportTicketRepository
{
    public Task<SupportTicket?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        db.SupportTickets.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);

    public async Task<SupportTicket> GetOrCreateForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserIdAsync(userId, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var ticket = new SupportTicket { UserId = userId };
        await db.SupportTickets.AddAsync(ticket, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public Task<SupportTicket?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.SupportTickets.AsNoTracking().Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<(IReadOnlyList<SupportTicketSummaryRow> Items, int Total)> GetAllForStaffAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query =
            from ticket in db.SupportTickets
            select new
            {
                Ticket = ticket,
                Last = ticket.Messages.OrderByDescending(m => m.CreatedAt).First(),
                UnreadCount = ticket.Messages.Count(m => !m.IsFromStaff && m.ReadAt == null),
            };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Last.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(x => new SupportTicketSummaryRow(x.Ticket.Id, x.Ticket.User.Name, x.Last.Text, x.Last.CreatedAt, x.UnreadCount))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
