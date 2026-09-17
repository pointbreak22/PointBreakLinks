using Identity.Domain.Common;

namespace Identity.Domain.Entities;

// Mirrors PasswordResetToken's design (deterministic SHA-256 hash, not bcrypt, since the token
// itself is the only lookup key — see that entity's comment). The pending NewEmail lives on the
// token, not on User, so an abandoned/expired request never partially changes anything: the only
// write to User.Email happens at confirm time, all at once.
public class EmailChangeToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string NewEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
}
