using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common;
using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.CompleteTwoFactorLogin;

public class CompleteTwoFactorLoginCommandHandler(
    ITwoFactorLoginTicketRepository ticketRepository,
    ITwoFactorBackupCodeRepository backupCodeRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    ILoginHistoryRepository loginHistoryRepository,
    ITotpService totpService,
    TokenIssuer tokenIssuer,
    NewDeviceLoginNotifier newDeviceLoginNotifier) : IRequestHandler<CompleteTwoFactorLoginCommand, AuthResultDto>
{
    private static readonly TimeSpan TrustedDeviceLifetime = TimeSpan.FromDays(30);

    public async Task<AuthResultDto> Handle(CompleteTwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Ticket)));
        var ticket = await ticketRepository.GetValidByHashAsync(tokenHash, cancellationToken)
                     ?? throw new AuthenticationException("Сессия входа истекла, попробуйте войти заново.");

        // Should be unreachable in practice (a ticket is only ever minted for a 2FA-enabled
        // user, see LoginCommandHandler), but a user could disable 2FA in another tab between
        // minting the ticket and submitting a code here — treat that the same as "no ticket".
        if (!ticket.User.TwoFactorEnabled || ticket.User.TwoFactorSecret == null)
        {
            throw new AuthenticationException("Сессия входа истекла, попробуйте войти заново.");
        }

        // Accept either a live TOTP code or one of the user's unused backup codes — the field is
        // free-text on the client, so try the fast/common path first and only fall back to a
        // backup-code lookup (and burn it) when the TOTP check fails.
        if (!totpService.ValidateCode(ticket.User.TwoFactorSecret, request.Code))
        {
            var backupCode = await backupCodeRepository.GetUnusedByHashAsync(ticket.UserId, BackupCodeGenerator.Hash(request.Code), cancellationToken)
                              ?? throw new AuthenticationException("Неверный код подтверждения.");

            backupCode.IsUsed = true;
            backupCode.UsedAt = DateTime.UtcNow;
            await backupCodeRepository.SaveChangesAsync(cancellationToken);
        }

        ticket.IsUsed = true;
        await ticketRepository.SaveChangesAsync(cancellationToken);

        var auth = await tokenIssuer.IssueAsync(ticket.User, cancellationToken);

        if (request.RememberDevice)
        {
            var rawDeviceToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var deviceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawDeviceToken)));
            var expiresAt = DateTime.UtcNow.Add(TrustedDeviceLifetime);

            await trustedDeviceRepository.AddAsync(
                new TrustedDevice { UserId = ticket.UserId, TokenHash = deviceHash, Label = request.DeviceLabel, ExpiresAt = expiresAt },
                cancellationToken);
            await trustedDeviceRepository.SaveChangesAsync(cancellationToken);

            auth = auth with { TrustedDeviceToken = rawDeviceToken, TrustedDeviceTokenExpiresAt = expiresAt };
        }

        // Must run before AddAsync below — otherwise this login's own entry would already be
        // there and the "have we seen this device" check would always say yes.
        await newDeviceLoginNotifier.NotifyIfNewDeviceAsync(ticket.User, request.IpAddress, request.DeviceLabel, cancellationToken);

        await loginHistoryRepository.AddAsync(
            new LoginHistoryEntry { UserId = ticket.UserId, IpAddress = request.IpAddress, UserAgent = request.DeviceLabel },
            cancellationToken);
        await loginHistoryRepository.SaveChangesAsync(cancellationToken);

        return auth;
    }
}
