using System.Security.Cryptography;
using System.Text;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.ConfirmEmailChange;

public class ConfirmEmailChangeCommandHandler(IEmailChangeTokenRepository emailChangeTokenRepository, IUserRepository userRepository)
    : IRequestHandler<ConfirmEmailChangeCommand>
{
    public async Task Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var changeToken = await emailChangeTokenRepository.GetValidByHashAsync(tokenHash, cancellationToken)
                           ?? throw new AuthenticationException("Ссылка для подтверждения email недействительна или устарела.");

        // Re-checked here, not just at request time — someone else could have registered the
        // same address in the hour between requesting and confirming.
        if (await userRepository.EmailExistsAsync(changeToken.NewEmail, cancellationToken))
        {
            throw new ConflictException("Этот email уже используется другим аккаунтом.");
        }

        changeToken.User.Email = changeToken.NewEmail;
        changeToken.IsUsed = true;
        await emailChangeTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
