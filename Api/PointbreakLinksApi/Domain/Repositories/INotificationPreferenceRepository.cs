using Domain.Entities;

namespace Domain.Repositories;

public interface INotificationPreferenceRepository
{
    // Creates an all-enabled row on first access — see NotificationPreference's comment.
    Task<NotificationPreference> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
