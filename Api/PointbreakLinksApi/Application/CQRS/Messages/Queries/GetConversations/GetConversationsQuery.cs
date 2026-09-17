using Application.CQRS.Messages.DTOs;
using MediatR;

namespace Application.CQRS.Messages.Queries.GetConversations;

public record GetConversationsQuery(int UserId) : IRequest<IReadOnlyList<ConversationDto>>;
