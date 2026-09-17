using Identity.Application.Common;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Disable2Fa;

public class Disable2FaCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITwoFactorBackupCodeRepository backupCodeRepository,
    ITrustedDeviceRepository trustedDeviceRepository) : IRequestHandler<Disable2FaCommand>
{
    public async Task Handle(Disable2FaCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("User", request.UserId);

        // Re-checks the password rather than trusting the bearer token alone — disabling 2FA is
        // exactly the kind of action a stolen access token shouldn't be able to do by itself.
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Неверный пароль.");
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        // Backup codes only make sense while 2FA is on — drop them so a later re-enable always
        // starts from a clean, freshly minted batch instead of resurrecting stale ones.
        await backupCodeRepository.DeleteAllForUserAsync(user.Id, cancellationToken);
        // Same reasoning for trusted devices — a bypass for a step that no longer exists.
        await trustedDeviceRepository.DeleteAllForUserAsync(user.Id, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
