using Application.Common;
using Application.CQRS.Messages.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Messages.Commands.SendMessage;

public class SendMessageCommandHandler(
    IMessageRepository messageRepository,
    INotificationPusher notificationPusher,
    INotificationRepository notificationRepository)
    : IRequestHandler<SendMessageCommand, MessageDto>
{
    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var order = await messageRepository.GetOrderForParticipantAsync(request.PurchasedSiteId, request.SenderId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        var senderIsBuyer = order.BuyerId == request.SenderId;
        var recipientId = senderIsBuyer ? order.Site.SellerId : order.BuyerId;
        var senderName = senderIsBuyer ? order.Buyer.Name : order.Site.Seller.Name;

        var message = new Message
        {
            PurchasedSiteId = request.PurchasedSiteId,
            SenderId = request.SenderId,
            RecipientId = recipientId,
            Text = request.Text,
        };

        await messageRepository.AddAsync(message, cancellationToken);
        await messageRepository.SaveChangesAsync(cancellationToken);

        var dto = MessageDto.FromEntity(message);
        await notificationPusher.NotifyNewMessageAsync(recipientId, senderName, dto, cancellationToken);

        await notificationRepository.AddAsync(
            new Notification { UserId = recipientId, Message = $"Новое сообщение от {senderName}" },
            cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        return dto;
    }
}
