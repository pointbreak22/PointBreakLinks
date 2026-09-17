using Domain.Common;

namespace Domain.Entities;

// IsFromStaff instead of a fixed RecipientId (unlike Message) — a ticket's staff side isn't one
// fixed person, any admin/moderator can reply, so "read by the other side" only needs to know
// which side sent it, not track a specific recipient.
public class SupportMessage : BaseEntity
{
    public int SupportTicketId { get; set; }
    public SupportTicket SupportTicket { get; set; } = null!;

    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public bool IsFromStaff { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime? ReadAt { get; set; }
}
