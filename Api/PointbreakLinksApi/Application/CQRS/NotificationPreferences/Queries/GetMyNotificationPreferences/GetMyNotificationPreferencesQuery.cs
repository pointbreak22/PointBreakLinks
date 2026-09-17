using Application.CQRS.NotificationPreferences.DTOs;
using MediatR;

namespace Application.CQRS.NotificationPreferences.Queries.GetMyNotificationPreferences;

public record GetMyNotificationPreferencesQuery(int UserId) : IRequest<NotificationPreferenceDto>;
