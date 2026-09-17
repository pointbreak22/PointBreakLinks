using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common;
using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.CQRS.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITwoFactorLoginTicketRepository twoFactorTicketRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    ILoginHistoryRepository loginHistoryRepository,
    TokenIssuer tokenIssuer,
    NewDeviceLoginNotifier newDeviceLoginNotifier,
    IEmailSender emailSender,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, LoginResultDto>
{
    private static readonly TimeSpan TwoFactorTicketLifetime = TimeSpan.FromMinutes(5);

    // Brute-force protection: 5 wrong passwords in a row locks the account for 15 minutes,
    // regardless of whether a later attempt in that window would've been correct — blocking
    // login outright (not just re-counting) avoids leaking "was that password right?" via a
    // timing/behavior side channel during the lockout itself.
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user?.LockoutEndsAt is { } lockoutEndsAt && lockoutEndsAt > DateTime.UtcNow)
        {
            throw new AccountLockedException(BuildLockoutMessage(lockoutEndsAt));
        }

        if (user == null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user != null)
            {
                await RegisterFailedAttempt(user, cancellationToken);
            }

            throw new AuthenticationException("Invalid email or password.");
        }

        if (user.IsBanned)
        {
            throw new AuthenticationException("Аккаунт заблокирован.");
        }

        if (user.IsDeactivated)
        {
            throw new AuthenticationException("Аккаунт удалён.");
        }

        if (user.FailedLoginAttempts > 0 || user.LockoutEndsAt != null)
        {
            // LockoutEndsAt can still be a (now-past) timestamp here — a lockout that has simply
            // expired passes the check above without ever clearing it. Null it out on the first
            // successful login afterwards instead of leaving stale state around.
            user.FailedLoginAttempts = 0;
            user.LockoutEndsAt = null;
            await userRepository.SaveChangesAsync(cancellationToken);
        }

        if (user.TwoFactorEnabled)
        {
            if (request.TrustedDeviceToken != null)
            {
                var deviceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.TrustedDeviceToken)));
                var trustedDevice = await trustedDeviceRepository.GetValidByHashAsync(user.Id, deviceHash, cancellationToken);
                if (trustedDevice != null)
                {
                    var skipResult = await tokenIssuer.IssueAsync(user, cancellationToken);
                    await RecordLoginAsync(user, request, cancellationToken);
                    return LoginResultDto.Success(skipResult);
                }
            }

            // Password already checked out — this ticket proves that to
            // CompleteTwoFactorLoginCommand without issuing real tokens yet.
            var rawTicket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawTicket)));

            await twoFactorTicketRepository.AddAsync(
                new TwoFactorLoginTicket { UserId = user.Id, TokenHash = tokenHash, ExpiresAt = DateTime.UtcNow.Add(TwoFactorTicketLifetime) },
                cancellationToken);
            await twoFactorTicketRepository.SaveChangesAsync(cancellationToken);

            return LoginResultDto.NeedsTwoFactor(rawTicket);
        }

        var result = await tokenIssuer.IssueAsync(user, cancellationToken);
        await RecordLoginAsync(user, request, cancellationToken);
        return LoginResultDto.Success(result);
    }

    private async Task RecordLoginAsync(User user, LoginCommand request, CancellationToken cancellationToken)
    {
        // Must run before AddAsync below — otherwise this login's own entry would already be
        // there and the "have we seen this device" check would always say yes.
        await newDeviceLoginNotifier.NotifyIfNewDeviceAsync(user, request.IpAddress, request.UserAgent, cancellationToken);

        await loginHistoryRepository.AddAsync(
            new LoginHistoryEntry { UserId = user.Id, IpAddress = request.IpAddress, UserAgent = request.UserAgent },
            cancellationToken);
        await loginHistoryRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task RegisterFailedAttempt(User user, CancellationToken cancellationToken)
    {
        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            user.LockoutEndsAt = DateTime.UtcNow.Add(LockoutDuration);
            // Reset here (not left to a later successful login) so the count starts clean once
            // the lockout window passes, rather than carrying over into the next 5-strike window.
            user.FailedLoginAttempts = 0;
            await userRepository.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Account {Email} locked out until {LockoutEndsAt} after {Attempts} failed login attempts.",
                user.Email, user.LockoutEndsAt, MaxFailedLoginAttempts);

            await emailSender.SendAsync(
                user.Email,
                "Обнаружена подозрительная активность — PointbreakLinks",
                $"""
                 <p>Здравствуйте, {user.Name}!</p>
                 <p>Зафиксировано {MaxFailedLoginAttempts} неудачных попыток входа в ваш аккаунт подряд.
                 В целях безопасности вход временно заблокирован до {user.LockoutEndsAt:HH:mm dd.MM.yyyy} (UTC).</p>
                 <p>Если это были не вы, рекомендуем сменить пароль после разблокировки.</p>
                 """,
                cancellationToken);

            throw new AccountLockedException(BuildLockoutMessage(user.LockoutEndsAt.Value));
        }

        await userRepository.SaveChangesAsync(cancellationToken);
    }

    private static string BuildLockoutMessage(DateTime lockoutEndsAt)
    {
        var minutesLeft = Math.Max(1, (int)Math.Ceiling((lockoutEndsAt - DateTime.UtcNow).TotalMinutes));
        return $"Аккаунт временно заблокирован из-за большого числа неудачных попыток входа. Повторите через {minutesLeft} мин.";
    }
}
