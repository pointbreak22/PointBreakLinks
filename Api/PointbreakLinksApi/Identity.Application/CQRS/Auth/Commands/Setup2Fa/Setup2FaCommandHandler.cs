using Identity.Application.Common;
using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Setup2Fa;

public class Setup2FaCommandHandler(IUserRepository userRepository, ITotpService totpService)
    : IRequestHandler<Setup2FaCommand, TwoFactorSetupDto>
{
    private const string Issuer = "PointbreakLinks";

    public async Task<TwoFactorSetupDto> Handle(Setup2FaCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("User", request.UserId);

        if (user.TwoFactorEnabled)
        {
            throw new ConflictException("Двухфакторная аутентификация уже включена. Сначала отключите её.");
        }

        // Overwrites any earlier unconfirmed secret from a previous, abandoned setup attempt —
        // only one QR code should ever be valid to scan at a time.
        var secret = totpService.GenerateSecret();
        user.TwoFactorSecret = secret;
        await userRepository.SaveChangesAsync(cancellationToken);

        return new TwoFactorSetupDto(secret, totpService.BuildOtpAuthUri(secret, user.Email, Issuer));
    }
}
