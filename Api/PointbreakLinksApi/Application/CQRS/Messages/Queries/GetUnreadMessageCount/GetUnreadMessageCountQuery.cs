using MediatR;

namespace Application.CQRS.Messages.Queries.GetUnreadMessageCount;

public record GetUnreadMessageCountQuery(int UserId) : IRequest<int>;
