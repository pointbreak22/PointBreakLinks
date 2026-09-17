using Identity.Domain.Common;

namespace Identity.Domain.Entities;

// Minted by CompleteTwoFactorLoginCommandHandler when the user checks "Запомнить это
// устройство" — a long-lived (30 day), single-purpose token stored in its own httpOnly cookie
// (separate from the refresh-token cookie) that lets LoginCommandHandler skip the TOTP/backup-
// code step entirely on a later login from the same browser. Only ever consulted when the
// account actually has 2FA enabled; disabling 2FA wipes every row for the user (Disable2Fa
// CommandHandler), same cleanup TwoFactorBackupCode already gets.
public class TrustedDevice : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    // Best-effort label from the User-Agent header at trust time, purely for the user's own
    // "Доверенные устройства" list — never parsed/trusted for anything security-relevant.
    public string? Label { get; set; }

    public DateTime ExpiresAt { get; set; }
}
