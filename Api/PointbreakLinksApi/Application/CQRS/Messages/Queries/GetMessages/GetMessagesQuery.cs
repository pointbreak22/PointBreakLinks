using Application.CQRS.Messages.DTOs;
using MediatR;

namespace Application.CQRS.Messages.Queries.GetMessages;

public record GetMessagesQuery(int PurchasedSiteId, int ViewerId) : IRequest<IReadOnlyList<MessageDto>>;
