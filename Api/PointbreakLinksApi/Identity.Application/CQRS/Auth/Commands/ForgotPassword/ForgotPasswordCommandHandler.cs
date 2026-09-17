using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Application.CQRS.Auth.Commands.ForgotPassword;

// A local SMTP catcher (smtp4dev) is configured for the reset email — see EmailSettings/
// SmtpEmailSender in Identity.Infrastructure — so the flow is real end-to-end (token
// generation, hashing, expiry, single-use, delivery) rather than a stub. Still logged
// alongside the send purely as a developer convenience (no need to open the smtp4dev UI just
// to grab a token while testing).
public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository resetTokenRepository,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<ForgotPasswordCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        // Always succeed from the caller's point of view, whether or not the email exists —
        // otherwise this endpoint becomes a way to check which emails are registered.
        if (user == null)
        {
            return;
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        await resetTokenRepository.AddAsync(
            new PasswordResetToken { UserId = user.Id, TokenHash = tokenHash, ExpiresAt = DateTime.UtcNow.Add(TokenLifetime) },
            cancellationToken);
        await resetTokenRepository.SaveChangesAsync(cancellationToken);

        var clientBaseUrl = configuration.GetSection("AllowedOrigins").Get<string[]>()?.FirstOrDefault() ?? "http://localhost:4200";
        var resetLink = $"{clientBaseUrl}/reset-password?token={rawToken}";

        logger.LogInformation("Password reset requested for {Email}. Reset link: {Link}", user.Email, resetLink);

        await emailSender.SendAsync(
            user.Email,
            "Восстановление пароля PointbreakLinks",
            $"""
             <p>Здравствуйте, {user.Name}!</p>
             <p>Для сброса пароля перейдите по ссылке ниже (действительна 1 час):</p>
             <p><a href="{resetLink}">{resetLink}</a></p>
             <p>Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.</p>
             """,
            cancellationToken);
    }
}
