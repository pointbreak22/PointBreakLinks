using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class EfEmailChangeTokenRepository(IdentityDbContext db) : IEmailChangeTokenRepository
{
    public async Task AddAsync(EmailChangeToken token, CancellationToken cancellationToken = default) =>
        await db.EmailChangeTokens.AddAsync(token, cancellationToken);

    public Task<EmailChangeToken?> GetValidByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.EmailChangeTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
