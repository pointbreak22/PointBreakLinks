using Identity.Domain.Entities;
using Identity.Domain.Repositories;

namespace Identity.Application.Common;

// Shared by Login/CompleteTwoFactorLogin handlers so every successful-login path runs the same
// "have we seen this IP+UserAgent for this user before" check, ahead of the call that would
// otherwise mark it as seen (see each handler's own login-history recording). Mirrors
// LoginCommandHandler's existing lockout-notice email, but for an ordinary successful login from
// an unrecognized device rather than a brute-force block.
public class NewDeviceLoginNotifier(ILoginHistoryRepository loginHistoryRepository, IEmailSender emailSender)
{
    public async Task NotifyIfNewDeviceAsync(User user, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        // Can't meaningfully recognize a device without an IP — skip rather than risk alerting on
        // every single login (e.g. a client that never reports one).
        if (ipAddress == null)
        {
            return;
        }

        var isKnownDevice = await loginHistoryRepository.ExistsForDeviceAsync(user.Id, ipAddress, userAgent, cancellationToken);
        if (isKnownDevice)
        {
            return;
        }

        await emailSender.SendAsync(
            user.Email,
            "Вход с нового устройства — PointbreakLinks",
            $"""
             <p>Здравствуйте, {user.Name}!</p>
             <p>Зафиксирован вход в ваш аккаунт с устройства, которое мы раньше не видели.</p>
             <p>IP: {ipAddress}<br/>Устройство: {userAgent ?? "не определено"}<br/>Время: {DateTime.UtcNow:HH:mm dd.MM.yyyy} (UTC)</p>
             <p>Если это были не вы, срочно смените пароль и включите двухфакторную аутентификацию.</p>
             """,
            cancellationToken);
    }
}
