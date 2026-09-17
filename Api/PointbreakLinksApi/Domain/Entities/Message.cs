using Domain.Common;

namespace Domain.Entities;

// Buyer <-> seller chat scoped to one order (purchased_site_id), not a general DM.
public class Message : BaseEntity
{
    public int PurchasedSiteId { get; set; }
    public PurchasedSite PurchasedSite { get; set; } = null!;

    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public int RecipientId { get; set; }
    public User Recipient { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public DateTime? ReadAt { get; set; }

    // Set together or not at all. AttachmentPath is the on-disk name under wwwroot/uploads/
    // messages — always a fresh GUID, never the client-supplied original name, so a crafted
    // filename can't traverse or collide (see MessagesController). AttachmentFileName is that
    // original name, kept only for display/download purposes.
    public string? AttachmentPath { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
}
