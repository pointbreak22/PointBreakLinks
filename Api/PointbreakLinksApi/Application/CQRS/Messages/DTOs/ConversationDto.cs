namespace Application.CQRS.Messages.DTOs;

// One row per order that has at least one message — the "counterparty" is whichever party
// isn't the viewer (buyer sees the seller and vice versa), resolved server-side since neither
// SiteDto nor PurchasedSiteDto carries a seller name today (only the seller's bare id).
public record ConversationDto(
    int PurchasedSiteId,
    string SiteUrl,
    int CounterpartyId,
    string CounterpartyName,
    string LastMessageText,
    string LastMessageAt,
    int UnreadCount);
