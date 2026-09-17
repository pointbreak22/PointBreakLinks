using Domain.Common;

namespace Domain.Entities;

// A support conversation between one user and "the support team" (any admin/moderator) —
// deliberately not tied to a PurchasedSite/order like Message is (see Message.cs's comment):
// this is for general questions, not order-specific chat. One ticket per user, reused across
// their whole history with support rather than opening a new one per question — simplest model
// that still lets staff see "everything this user has ever asked."
public class SupportTicket : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<SupportMessage> Messages { get; set; } = [];
}
