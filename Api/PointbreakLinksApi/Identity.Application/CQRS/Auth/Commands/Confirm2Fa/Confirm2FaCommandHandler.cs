using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Confirm2Fa;

public class Confirm2FaCommandHandler(
    IUserRepository userRepository,
    ITotpService totpService,
    ITwoFactorBackupCodeRepository backupCodeRepository) : IRequestHandler<Confirm2FaCommand, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(Confirm2FaCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("User", request.UserId);

        if (user.TwoFactorSecret == null)
        {
            throw new ConflictException("Сначала начните настройку двухфакторной аутентификации.");
        }

        if (!totpService.ValidateCode(user.TwoFactorSecret, request.Code))
        {
            throw new AuthenticationException("Неверный код подтверждения.");
        }

        user.TwoFactorEnabled = true;

        var codes = BackupCodeGenerator.GenerateCodes();
        await backupCodeRepository.AddRangeAsync(
            codes.Select(code => new TwoFactorBackupCode { UserId = user.Id, CodeHash = BackupCodeGenerator.Hash(code) }),
            cancellationToken);

        // Same IdentityDbContext instance behind both repositories (scoped per request), so one
        // SaveChanges persists the user flip and the new backup-code rows together.
        await userRepository.SaveChangesAsync(cancellationToken);
        return codes;
    }
}
