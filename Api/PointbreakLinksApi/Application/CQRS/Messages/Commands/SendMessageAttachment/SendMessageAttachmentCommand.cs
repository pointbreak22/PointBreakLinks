using Application.CQRS.Messages.DTOs;
using MediatR;

namespace Application.CQRS.Messages.Commands.SendMessageAttachment;

// The file itself is already saved to disk by the time this runs (MessagesController does that
// I/O, then hands over the resulting on-disk name) — this only ever persists metadata, same
// split HttpSiteVerificationService's SSRF-hardened fetch keeps out of Application entirely.
public record SendMessageAttachmentCommand(
    int PurchasedSiteId,
    int SenderId,
    string? Text,
    string AttachmentPath,
    string AttachmentFileName,
    string AttachmentContentType) : IRequest<MessageDto>;
