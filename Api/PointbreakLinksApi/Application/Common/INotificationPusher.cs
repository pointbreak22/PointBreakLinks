using Application.CQRS.Messages.DTOs;
using Application.CQRS.Support.DTOs;

namespace Application.Common;

// Live, best-effort push over SignalR — implemented in WebAPI (where NotificationHub actually
// lives; Application can't reference it directly) and injected into the command handlers whose
// action should notify someone in real time. Not from FOXLinks: `NotificationHub` was scaffolded
// from the start of this port but never wired to anything until now. Deliberately fire-and-thin:
// a disconnected client or a hub hiccup must never fail or roll back the command that triggered
// it — see SignalRNotificationPusher's comment for how that's kept true.
public interface INotificationPusher
{
    Task NotifyOrderReceivedAsync(int sellerId, string siteUrl, CancellationToken cancellationToken = default);
    Task NotifyOrderAcceptedAsync(int buyerId, string siteUrl, CancellationToken cancellationToken = default);
    Task NotifyOrderCancelledAsync(int sellerId, string siteUrl, CancellationToken cancellationToken = default);
    Task NotifyOrderDeclinedAsync(int buyerId, string siteUrl, CancellationToken cancellationToken = default);

    // Carries the full MessageDto (not just senderName) so an already-open MessageChatModal for
    // this exact order can append the real message live instead of only surfacing a toast — see
    // shared/message-chat-modal/message-chat-modal.ts's NotificationHubService subscription.
    Task NotifyNewMessageAsync(int recipientId, string senderName, MessageDto message, CancellationToken cancellationToken = default);

    Task NotifySiteModeratedAsync(int sellerId, string siteUrl, bool approved, CancellationToken cancellationToken = default);

    // Pushed only in the staff-reply direction (see SendSupportReplyCommandHandler) — a user's
    // own message to support has no live push to staff (no per-staff-member connection to
    // target; staff refetch the ticket list instead, same as every other admin list page).
    Task NotifyNewSupportMessageAsync(int userId, SupportMessageDto message, CancellationToken cancellationToken = default);
}
