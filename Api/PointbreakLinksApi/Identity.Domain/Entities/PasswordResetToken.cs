using Identity.Domain.Common;

namespace Identity.Domain.Entities;

// New entity, not from FOXLinks — there was no password-reset flow anywhere in the source.
// TokenHash is a deterministic SHA-256 of the raw token (not bcrypt) so a reset link can be
// looked up by exact match — bcrypt's random salt makes that kind of lookup impossible, which
// is fine for a login password (checked one at a time, id known already) but wrong here since
// the token IS the only lookup key.
public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;
}
