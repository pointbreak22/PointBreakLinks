using Domain.Entities;

namespace Domain.Repositories;

// Admin ticket-list row — a flat projection, same reasoning as ConversationDto in
// IMessageRepository.cs.
public record SupportTicketSummaryRow(int Id, string UserName, string LastMessageText, DateTime LastMessageAt, int UnreadCount);

public interface ISupportTicketRepository
{
    // Read-only lookup — used by GetMySupportThreadQuery, which must NOT create a ticket just
    // because a user opened the page with nothing written yet (that would clutter the staff
    // ticket list with empty rows). Only SendSupportMessageCommand (via GetOrCreateForUserAsync)
    // actually creates one, lazily, on the user's first message.
    Task<SupportTicket?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<SupportTicket> GetOrCreateForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Admin/moderator-only — every ticket platform-wide, most recently active first. Every row
    // has ≥1 message by construction (see GetOrCreateForUserAsync's comment), so
    // LastMessageText/LastMessageAt are never fabricated placeholders.
    Task<(IReadOnlyList<SupportTicketSummaryRow> Items, int Total)> GetAllForStaffAsync(int page, int perPage, CancellationToken cancellationToken = default);
}
