using Domain.Entities;

namespace Domain.Repositories;

public interface ISupportMessageRepository
{
    Task<List<SupportMessage>> GetByTicketAsync(int ticketId, CancellationToken cancellationToken = default);
    Task AddAsync(SupportMessage message, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Marks every unread staff reply on the ticket as read — called when the ticket's owner
    // views their own thread.
    Task MarkStaffMessagesReadAsync(int ticketId, CancellationToken cancellationToken = default);

    // Marks every unread user message on the ticket as read — called when a staff member views
    // the thread (any admin/moderator counts as "staff has seen it," there's no per-staff-member
    // read state).
    Task MarkUserMessagesReadAsync(int ticketId, CancellationToken cancellationToken = default);
}
