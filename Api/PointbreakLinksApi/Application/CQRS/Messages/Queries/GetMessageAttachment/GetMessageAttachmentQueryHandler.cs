using Application.CQRS.Messages.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Messages.Queries.GetMessageAttachment;

public class GetMessageAttachmentQueryHandler(IMessageRepository messageRepository)
    : IRequestHandler<GetMessageAttachmentQuery, MessageAttachmentDto>
{
    public async Task<MessageAttachmentDto> Handle(GetMessageAttachmentQuery request, CancellationToken cancellationToken)
    {
        // Confirms the viewer is actually a participant (buyer or seller) of this order before
        // trusting anything else — same check every other message endpoint makes.
        _ = await messageRepository.GetOrderForParticipantAsync(request.PurchasedSiteId, request.ViewerId, cancellationToken)
            ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        var message = await messageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message == null || message.PurchasedSiteId != request.PurchasedSiteId || message.AttachmentPath == null)
        {
            throw new NotFoundException("MessageAttachment", request.MessageId);
        }

        return new MessageAttachmentDto(message.AttachmentPath, message.AttachmentFileName!, message.AttachmentContentType!);
    }
}
