using Domain.Entities;

namespace Application.CQRS.Notifications.DTOs;

public record NotificationDto(int Id, string Message, bool IsRead, DateTime CreatedAt)
{
    public static NotificationDto FromEntity(Notification notification) =>
        new(notification.Id, notification.Message, notification.IsRead, notification.CreatedAt);
}
