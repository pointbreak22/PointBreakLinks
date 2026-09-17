using Domain.Entities;

namespace Application.CQRS.Messages.DTOs;

public record MessageDto(
    int Id,
    int PurchasedSiteId,
    int SenderId,
    int RecipientId,
    string Text,
    string CreatedAt,
    bool IsRead,
    // Null unless the message carries a file — the client builds the download URL itself
    // (ApiEndpoints.messages.attachment) rather than this DTO embedding a full path.
    string? AttachmentFileName,
    // Lets the client decide whether to render an inline image preview vs. a generic
    // paperclip/filename download button, without guessing from the extension.
    string? AttachmentContentType)
{
    public static MessageDto FromEntity(Message message) => new(
        message.Id,
        message.PurchasedSiteId,
        message.SenderId,
        message.RecipientId,
        message.Text,
        message.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        message.ReadAt != null,
        message.AttachmentFileName,
        message.AttachmentContentType);
}
