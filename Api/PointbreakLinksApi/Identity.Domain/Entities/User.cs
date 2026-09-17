using Identity.Domain.Common;

namespace Identity.Domain.Entities;

// The Identity module's own copy of the "users" table (schema "identity", same physical
// Postgres database as the business data — see PROJECT_MAP.md's write-up on why a schema split
// was chosen over a separate database: real FK constraints from business tables to this table
// still work, since Postgres FKs survive `ALTER TABLE ... SET SCHEMA` and work across schemas
// within one database). Auth-only fields live here (PasswordHash) — Balance moved out to the
// business-owned Wallet entity, since it's a marketplace concern, not an identity one.
// The business Domain has its own, separately-mapped `User` class pointing at this same table
// (Id/Name/Email/IsBanned/Roles/Projects, no PasswordHash) for display and admin purposes —
// see Domain/Entities/User.cs's comment for why that duplication is the right call here.
public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsBanned { get; set; }

    // Self-service deletion (DeactivateAccountCommand) — soft delete: blocks login same as
    // IsBanned, but distinct from it since it's the user's own choice, not a moderation action.
    public bool IsDeactivated { get; set; }

    // Set by Setup2FaCommand as soon as a QR code is generated, before the user has proven they
    // can actually produce a code from it — TwoFactorEnabled only flips to true once
    // Confirm2FaCommand validates a real code, so a half-finished setup never blocks login.
    public string? TwoFactorSecret { get; set; }
    public bool TwoFactorEnabled { get; set; }

    // Brute-force lockout — see LoginCommandHandler. FailedLoginAttempts resets to 0 on any
    // successful password check (including the one that triggers a lockout itself, so the count
    // starts fresh once LockoutEndsAt passes) and LockoutEndsAt is null whenever the account
    // isn't currently locked out.
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndsAt { get; set; }

    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
