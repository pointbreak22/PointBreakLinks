using Identity.Domain.Common;

namespace Identity.Domain.Entities;

// One-time recovery codes minted alongside 2FA enable/regenerate (see Confirm2FaCommandHandler /
// RegenerateBackupCodesCommandHandler) so a user who loses their authenticator device isn't
// permanently locked out of an account that requires it. Only the SHA-256 hash is ever
// persisted — CodeHash mirrors TwoFactorLoginTicket.TokenHash's reasoning exactly: opaque,
// single-use, never round-tripped in plaintext once shown to the user at mint time.
public class TwoFactorBackupCode : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string CodeHash { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
}
