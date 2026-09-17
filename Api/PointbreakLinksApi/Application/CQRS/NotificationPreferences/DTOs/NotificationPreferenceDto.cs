using Domain.Entities;

namespace Application.CQRS.NotificationPreferences.DTOs;

public record NotificationPreferenceDto(bool EmailOnOrderUpdates, bool EmailOnDisputeUpdates)
{
    public static NotificationPreferenceDto FromEntity(NotificationPreference preference) =>
        new(preference.EmailOnOrderUpdates, preference.EmailOnDisputeUpdates);
}
