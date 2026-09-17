using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface ITwoFactorLoginTicketRepository
{
    Task AddAsync(TwoFactorLoginTicket ticket, CancellationToken cancellationToken = default);

    // Requires User loaded — the caller reads User.TwoFactorSecret directly off the result.
    Task<TwoFactorLoginTicket?> GetValidByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
