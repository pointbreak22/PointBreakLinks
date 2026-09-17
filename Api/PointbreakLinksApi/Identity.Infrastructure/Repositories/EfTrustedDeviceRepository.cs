using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class EfTrustedDeviceRepository(IdentityDbContext db) : ITrustedDeviceRepository
{
    public async Task AddAsync(TrustedDevice device, CancellationToken cancellationToken = default) =>
        await db.TrustedDevices.AddAsync(device, cancellationToken);

    public Task<TrustedDevice?> GetValidByHashAsync(int userId, string tokenHash, CancellationToken cancellationToken = default) =>
        db.TrustedDevices.FirstOrDefaultAsync(
            d => d.UserId == userId && d.TokenHash == tokenHash && d.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public async Task<IReadOnlyList<TrustedDevice>> GetByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.TrustedDevices.Where(d => d.UserId == userId).OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);

    public Task<TrustedDevice?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default) =>
        db.TrustedDevices.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, cancellationToken);

    public Task DeleteAsync(TrustedDevice device, CancellationToken cancellationToken = default)
    {
        db.TrustedDevices.Remove(device);
        return Task.CompletedTask;
    }

    public async Task DeleteAllForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var devices = await db.TrustedDevices.Where(d => d.UserId == userId).ToListAsync(cancellationToken);
        db.TrustedDevices.RemoveRange(devices);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
