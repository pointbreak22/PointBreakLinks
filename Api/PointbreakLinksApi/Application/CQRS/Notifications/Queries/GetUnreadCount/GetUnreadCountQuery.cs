using MediatR;

namespace Application.CQRS.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery(int UserId) : IRequest<int>;
