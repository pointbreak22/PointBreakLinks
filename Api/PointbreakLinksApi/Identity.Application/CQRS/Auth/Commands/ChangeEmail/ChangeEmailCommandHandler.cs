using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Application.CQRS.Auth.Commands.ChangeEmail;

// Same "confirm before it takes effect" shape as ForgotPassword/ResetPassword, except the
// confirmation link is sent to the NEW address rather than an existing one — that's the whole
// point: it proves the user actually controls the mailbox they're switching to, not just that
// they know their own password.
public class ChangeEmailCommandHandler(
    IUserRepository userRepository,
    IEmailChangeTokenRepository emailChangeTokenRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<ChangeEmailCommandHandler> logger) : IRequestHandler<ChangeEmailCommand>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("User", request.UserId);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Неверный пароль.");
        }

        if (string.Equals(request.NewEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Это уже ваш текущий email.");
        }

        if (await userRepository.EmailExistsAsync(request.NewEmail, cancellationToken))
        {
            throw new ConflictException("Этот email уже используется другим аккаунтом.");
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        await emailChangeTokenRepository.AddAsync(
            new EmailChangeToken { UserId = user.Id, NewEmail = request.NewEmail, TokenHash = tokenHash, ExpiresAt = DateTime.UtcNow.Add(TokenLifetime) },
            cancellationToken);
        await emailChangeTokenRepository.SaveChangesAsync(cancellationToken);

        var clientBaseUrl = configuration.GetSection("AllowedOrigins").Get<string[]>()?.FirstOrDefault() ?? "http://localhost:4200";
        var confirmLink = $"{clientBaseUrl}/confirm-email-change?token={rawToken}";

        logger.LogInformation("Email change requested for user {UserId} to {NewEmail}. Confirm link: {Link}", user.Id, request.NewEmail, confirmLink);

        // Deliberately sent to the NEW address, not the old one — see class comment.
        await emailSender.SendAsync(
            request.NewEmail,
            "Подтверждение смены email — PointbreakLinks",
            $"""
             <p>Здравствуйте, {user.Name}!</p>
             <p>Для завершения смены email на этот адрес перейдите по ссылке ниже (действительна 1 час):</p>
             <p><a href="{confirmLink}">{confirmLink}</a></p>
             <p>Если вы не запрашивали смену email, просто проигнорируйте это письмо — текущий email останется без изменений.</p>
             """,
            cancellationToken);
    }
}
