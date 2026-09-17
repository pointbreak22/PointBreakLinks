using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler(INotificationRepository notificationRepository) : IRequestHandler<GetUnreadCountQuery, int>
{
    public Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken) =>
        notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);
}
