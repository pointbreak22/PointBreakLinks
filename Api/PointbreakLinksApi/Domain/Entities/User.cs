using Domain.Common;

namespace Domain.Entities;

// This is the BUSINESS side's own, separately-mapped view of the "users" table (schema
// "identity", owned by the Identity module — see Identity.Domain/Entities/User.cs and
// PROJECT_MAP.md's write-up on the schema split). No PasswordHash here — that's an Identity
// concern this module never touches. Business code still needs to read (Site.Seller,
// Project.User, Message.Sender/Recipient, Notification.User display names) and write
// (Admin ban/role management) against this same table, so it keeps its own mapping — real FK
// constraints to it still work since Postgres FKs are enforced across schemas within one
// database, they just don't require both sides to be modeled by the same DbContext.
// Balance moved out to Wallet (a business/marketplace concept, not an identity one).
public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsBanned { get; set; }

    // Brute-force lockout set by Identity's LoginCommandHandler — mapped read/write here too so
    // Admin can see it (AdminUserDto) and clear it early (UnlockUserCommand) without waiting out
    // the 15-minute window.
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndsAt { get; set; }

    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}
