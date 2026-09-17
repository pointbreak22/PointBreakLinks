using Identity.Domain.Common;

namespace Identity.Domain.Entities;

// Issued by LoginCommandHandler once email+password check out for a user with TwoFactorEnabled
// — proves "this specific request already passed the password check" to
// CompleteTwoFactorLoginCommand without letting the client hold anything more powerful than a
// short-lived, single-use, opaque ticket in the meantime (mirrors PasswordResetToken's
// TokenHash/ExpiresAt/IsUsed shape exactly, same reasoning).
public class TwoFactorLoginTicket : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
}
