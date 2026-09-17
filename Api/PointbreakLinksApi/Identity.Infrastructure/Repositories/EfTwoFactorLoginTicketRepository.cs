using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class EfTwoFactorLoginTicketRepository(IdentityDbContext db) : ITwoFactorLoginTicketRepository
{
    public async Task AddAsync(TwoFactorLoginTicket ticket, CancellationToken cancellationToken = default) =>
        await db.TwoFactorLoginTickets.AddAsync(ticket, cancellationToken);

    public Task<TwoFactorLoginTicket?> GetValidByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.TwoFactorLoginTickets
            .Include(t => t.User).ThenInclude(u => u.Roles)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
