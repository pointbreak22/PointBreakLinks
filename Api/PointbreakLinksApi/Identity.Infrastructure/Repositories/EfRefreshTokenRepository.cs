using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class EfRefreshTokenRepository(IdentityDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

    public async Task UpsertForUserAsync(int userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId, cancellationToken);
        if (existing != null)
        {
            existing.Token = token;
            existing.ExpiresAt = expiresAt;
        }
        else
        {
            await db.RefreshTokens.AddAsync(new RefreshToken { UserId = userId, Token = token, ExpiresAt = expiresAt }, cancellationToken);
        }
    }

    public async Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        if (existing != null)
        {
            db.RefreshTokens.Remove(existing);
        }
    }

    public async Task DeleteForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var tokens = await db.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync(cancellationToken);
        db.RefreshTokens.RemoveRange(tokens);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
