using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfNotificationRepository(ApplicationDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default) =>
        await db.Notifications.AddAsync(notification, cancellationToken);

    public async Task<(IReadOnlyList<Notification> Items, int Total)> GetByUserAsync(int userId, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var query = db.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ExecuteUpdateAsync(
            setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
