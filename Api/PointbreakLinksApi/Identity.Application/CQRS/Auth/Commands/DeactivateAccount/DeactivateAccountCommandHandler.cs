using Identity.Application.Common;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.DeactivateAccount;

public class DeactivateAccountCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IRefreshTokenRepository refreshTokenRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    ITwoFactorBackupCodeRepository backupCodeRepository) : IRequestHandler<DeactivateAccountCommand>
{
    public async Task Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("User", request.UserId);

        // Re-checks the password rather than trusting the bearer token alone — same reasoning as
        // Disable2FaCommandHandler: a stolen access token shouldn't be able to delete the account.
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Неверный пароль.");
        }

        user.IsDeactivated = true;
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;

        await backupCodeRepository.DeleteAllForUserAsync(user.Id, cancellationToken);
        await trustedDeviceRepository.DeleteAllForUserAsync(user.Id, cancellationToken);
        await refreshTokenRepository.DeleteForUserAsync(user.Id, cancellationToken);

        // All four repositories share the same scoped IdentityDbContext (see
        // Disable2FaCommandHandler's identical single-SaveChanges pattern) — one call persists
        // the user flag flip and every removal queued above in one transaction.
        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
