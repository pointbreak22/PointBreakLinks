using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfNotificationPreferenceRepository(ApplicationDbContext db) : INotificationPreferenceRepository
{
    public async Task<NotificationPreference> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var preference = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (preference != null)
        {
            return preference;
        }

        preference = new NotificationPreference { UserId = userId };
        await db.NotificationPreferences.AddAsync(preference, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return preference;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
