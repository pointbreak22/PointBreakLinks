using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfSupportMessageRepository(ApplicationDbContext db) : ISupportMessageRepository
{
    public Task<List<SupportMessage>> GetByTicketAsync(int ticketId, CancellationToken cancellationToken = default) =>
        db.SupportMessages
            .Include(m => m.Sender)
            .Where(m => m.SupportTicketId == ticketId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SupportMessage message, CancellationToken cancellationToken = default) =>
        await db.SupportMessages.AddAsync(message, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task MarkStaffMessagesReadAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        await db.SupportMessages
            .Where(m => m.SupportTicketId == ticketId && m.IsFromStaff && m.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.ReadAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task MarkUserMessagesReadAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        await db.SupportMessages
            .Where(m => m.SupportTicketId == ticketId && !m.IsFromStaff && m.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.ReadAt, DateTime.UtcNow), cancellationToken);
    }
}
