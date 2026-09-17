using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler(IPasswordResetTokenRepository resetTokenRepository, IPasswordHasher passwordHasher)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            throw new ConflictException("Пароль должен быть не короче 8 символов и содержать заглавную букву и цифру.");
        }

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var resetToken = await resetTokenRepository.GetValidByHashAsync(tokenHash, cancellationToken)
                          ?? throw new AuthenticationException("Ссылка для сброса пароля недействительна или устарела.");

        resetToken.User.PasswordHash = passwordHasher.Hash(request.NewPassword);
        resetToken.IsUsed = true;
        await resetTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
