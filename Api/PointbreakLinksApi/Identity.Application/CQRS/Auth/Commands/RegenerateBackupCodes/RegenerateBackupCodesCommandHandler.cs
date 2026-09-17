using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.RegenerateBackupCodes;

public class RegenerateBackupCodesCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITwoFactorBackupCodeRepository backupCodeRepository) : IRequestHandler<RegenerateBackupCodesCommand, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(RegenerateBackupCodesCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("User", request.UserId);

        if (!user.TwoFactorEnabled)
        {
            throw new ConflictException("Двухфакторная аутентификация не включена.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Неверный пароль.");
        }

        await backupCodeRepository.DeleteAllForUserAsync(user.Id, cancellationToken);

        var codes = BackupCodeGenerator.GenerateCodes();
        await backupCodeRepository.AddRangeAsync(
            codes.Select(code => new TwoFactorBackupCode { UserId = user.Id, CodeHash = BackupCodeGenerator.Hash(code) }),
            cancellationToken);

        await backupCodeRepository.SaveChangesAsync(cancellationToken);
        return codes;
    }
}
