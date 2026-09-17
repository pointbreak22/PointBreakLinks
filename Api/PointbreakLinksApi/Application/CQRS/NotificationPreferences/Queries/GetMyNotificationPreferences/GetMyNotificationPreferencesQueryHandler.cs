using Application.CQRS.NotificationPreferences.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.NotificationPreferences.Queries.GetMyNotificationPreferences;

public class GetMyNotificationPreferencesQueryHandler(INotificationPreferenceRepository preferenceRepository)
    : IRequestHandler<GetMyNotificationPreferencesQuery, NotificationPreferenceDto>
{
    public async Task<NotificationPreferenceDto> Handle(GetMyNotificationPreferencesQuery request, CancellationToken cancellationToken) =>
        NotificationPreferenceDto.FromEntity(await preferenceRepository.GetOrCreateAsync(request.UserId, cancellationToken));
}
