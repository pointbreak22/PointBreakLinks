using Application.CQRS.NotificationPreferences.DTOs;
using MediatR;

namespace Application.CQRS.NotificationPreferences.Commands.UpdateNotificationPreferences;

public record UpdateNotificationPreferencesCommand(int UserId, bool EmailOnOrderUpdates, bool EmailOnDisputeUpdates)
    : IRequest<NotificationPreferenceDto>;
