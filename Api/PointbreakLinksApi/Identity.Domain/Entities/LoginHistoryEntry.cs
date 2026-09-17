using Identity.Domain.Common;

namespace Identity.Domain.Entities;

// One row per successful login (password-only, password+2FA, or password+trusted-device skip —
// see LoginCommandHandler/CompleteTwoFactorLoginCommandHandler), purely informational: nothing
// reads this to make a security decision, unlike TrustedDevice (which actually gates the 2FA
// step) or the account-lockout counters on User. This is just the "История входов" a user sees
// in their own profile — the last line of defense for noticing "that wasn't me."
public class LoginHistoryEntry : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
