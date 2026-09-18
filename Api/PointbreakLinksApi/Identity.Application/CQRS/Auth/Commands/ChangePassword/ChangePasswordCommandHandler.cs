using Identity.Application.Common;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITrustedDeviceRepository trustedDeviceRepository) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("User", request.UserId);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new AuthenticationException("Текущий пароль указан неверно.");
        }

        // New-password complexity enforced by ChangePasswordCommandValidator.
        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await userRepository.SaveChangesAsync(cancellationToken);

        // A password change is exactly the moment a "skip 2FA on this device" bypass should not
        // survive — if the change was prompted by a compromise, an old trusted-device cookie
        // would otherwise still let the attacker straight past 2FA on the next login.
        await trustedDeviceRepository.DeleteAllForUserAsync(user.Id, cancellationToken);
        await trustedDeviceRepository.SaveChangesAsync(cancellationToken);
    }
}
