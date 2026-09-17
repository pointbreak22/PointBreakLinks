using Application.CQRS.Messages.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Messages.Queries.GetConversations;

public class GetConversationsQueryHandler(IMessageRepository messageRepository) : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
{
    public async Task<IReadOnlyList<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var orders = await messageRepository.GetConversationsForUserAsync(request.UserId, cancellationToken);

        return orders
            .Select(order =>
            {
                var iAmBuyer = order.BuyerId == request.UserId;
                var counterparty = iAmBuyer ? order.Site.Seller : order.Buyer;
                var lastMessage = order.Messages.OrderByDescending(m => m.CreatedAt).First();
                var unreadCount = order.Messages.Count(m => m.RecipientId == request.UserId && m.ReadAt == null);

                return (lastMessage.CreatedAt, Dto: new ConversationDto(
                    order.Id,
                    order.Site.Url,
                    counterparty.Id,
                    counterparty.Name,
                    lastMessage.Text,
                    lastMessage.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    unreadCount));
            })
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Dto)
            .ToList();
    }
}
