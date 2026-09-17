using Application.Common;
using Application.CQRS.Notifications.DTOs;
using MediatR;

namespace Application.CQRS.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(int UserId, int Page, int PerPage) : IRequest<PagedResult<NotificationDto>>;
