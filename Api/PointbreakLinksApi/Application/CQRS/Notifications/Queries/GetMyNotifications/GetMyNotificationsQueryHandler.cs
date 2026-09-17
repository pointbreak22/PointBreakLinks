using Application.Common;
using Application.CQRS.Notifications.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler(INotificationRepository notificationRepository)
    : IRequestHandler<GetMyNotificationsQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await notificationRepository.GetByUserAsync(request.UserId, request.Page, request.PerPage, cancellationToken);
        var dtos = items.Select(NotificationDto.FromEntity).ToList();
        return PagedResult<NotificationDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
