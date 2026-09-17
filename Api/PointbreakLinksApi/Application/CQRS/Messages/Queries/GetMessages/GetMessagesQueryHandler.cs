using Application.CQRS.Messages.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Messages.Queries.GetMessages;

public class GetMessagesQueryHandler(IMessageRepository messageRepository) : IRequestHandler<GetMessagesQuery, IReadOnlyList<MessageDto>>
{
    public async Task<IReadOnlyList<MessageDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        // Confirms the viewer is actually a participant (buyer or seller) before returning
        // anything — chat is scoped per order, not a general inbox.
        _ = await messageRepository.GetOrderForParticipantAsync(request.PurchasedSiteId, request.ViewerId, cancellationToken)
            ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        var messages = await messageRepository.GetByPurchasedSiteAsync(request.PurchasedSiteId, cancellationToken);

        // Opening the chat marks the other participant's messages as read — this is what
        // makes the unread-dot in my-sales.vue's chat button real instead of always-false.
        var unreadForViewer = messages.Where(m => m.RecipientId == request.ViewerId && m.ReadAt == null).ToList();
        if (unreadForViewer.Count > 0)
        {
            foreach (var message in unreadForViewer)
            {
                message.ReadAt = DateTime.UtcNow;
            }
            await messageRepository.SaveChangesAsync(cancellationToken);
        }

        return messages.Select(MessageDto.FromEntity).ToList();
    }
}
