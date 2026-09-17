using Application.CQRS.NotificationPreferences.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.NotificationPreferences.Commands.UpdateNotificationPreferences;

public class UpdateNotificationPreferencesCommandHandler(INotificationPreferenceRepository preferenceRepository)
    : IRequestHandler<UpdateNotificationPreferencesCommand, NotificationPreferenceDto>
{
    public async Task<NotificationPreferenceDto> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var preference = await preferenceRepository.GetOrCreateAsync(request.UserId, cancellationToken);
        preference.EmailOnOrderUpdates = request.EmailOnOrderUpdates;
        preference.EmailOnDisputeUpdates = request.EmailOnDisputeUpdates;
        await preferenceRepository.SaveChangesAsync(cancellationToken);
        return NotificationPreferenceDto.FromEntity(preference);
    }
}
