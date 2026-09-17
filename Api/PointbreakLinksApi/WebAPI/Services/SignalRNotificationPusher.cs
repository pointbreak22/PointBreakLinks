using Application.Common;
using Application.CQRS.Messages.DTOs;
using Application.CQRS.Support.DTOs;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Services;

public class SignalRNotificationPusher(IHubContext<NotificationHub> hub, ILogger<SignalRNotificationPusher> logger) : INotificationPusher
{
    public Task NotifyOrderReceivedAsync(int sellerId, string siteUrl, CancellationToken cancellationToken = default) =>
        SendAsync(sellerId, "OrderReceived", new { siteUrl }, cancellationToken);

    public Task NotifyOrderAcceptedAsync(int buyerId, string siteUrl, CancellationToken cancellationToken = default) =>
        SendAsync(buyerId, "OrderAccepted", new { siteUrl }, cancellationToken);

    public Task NotifyOrderCancelledAsync(int sellerId, string siteUrl, CancellationToken cancellationToken = default) =>
        SendAsync(sellerId, "OrderCancelled", new { siteUrl }, cancellationToken);

    public Task NotifyOrderDeclinedAsync(int buyerId, string siteUrl, CancellationToken cancellationToken = default) =>
        SendAsync(buyerId, "OrderDeclined", new { siteUrl }, cancellationToken);

    // Payload shape matches the client's MessageDto exactly (id/purchasedSiteId/senderId/
    // recipientId/text/createdAt/isRead) plus senderName, tacked on only for the toast text —
    // lets MessageChatModal push the payload straight into its message list with no mapping.
    public Task NotifyNewMessageAsync(int recipientId, string senderName, MessageDto message, CancellationToken cancellationToken = default) =>
        SendAsync(recipientId, "NewMessage", new
        {
            senderName,
            message.Id,
            message.PurchasedSiteId,
            message.SenderId,
            message.RecipientId,
            message.Text,
            message.CreatedAt,
            message.IsRead,
            message.AttachmentFileName,
        }, cancellationToken);

    public Task NotifySiteModeratedAsync(int sellerId, string siteUrl, bool approved, CancellationToken cancellationToken = default) =>
        SendAsync(sellerId, "SiteModerated", new { siteUrl, approved }, cancellationToken);

    public Task NotifyNewSupportMessageAsync(int userId, SupportMessageDto message, CancellationToken cancellationToken = default) =>
        SendAsync(userId, "NewSupportMessage", message, cancellationToken);

    // A disconnected recipient (offline, no active hub connection) or a transient hub error must
    // never fail the command that triggered it — the order/message/moderation action already
    // succeeded and was already saved by the time this runs; swallow and log instead of throwing.
    private async Task SendAsync(int userId, string method, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync(method, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push {Method} notification to user {UserId}", method, userId);
        }
    }
}
