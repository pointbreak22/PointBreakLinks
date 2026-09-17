using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IEmailChangeTokenRepository
{
    Task AddAsync(EmailChangeToken token, CancellationToken cancellationToken = default);

    // Requires User loaded — the caller updates User.Email directly off the result.
    Task<EmailChangeToken?> GetValidByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
