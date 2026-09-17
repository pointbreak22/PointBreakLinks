using Application.CQRS.Messages.DTOs;
using MediatR;

namespace Application.CQRS.Messages.Commands.SendMessage;

public record SendMessageCommand(int PurchasedSiteId, int SenderId, string Text) : IRequest<MessageDto>;
