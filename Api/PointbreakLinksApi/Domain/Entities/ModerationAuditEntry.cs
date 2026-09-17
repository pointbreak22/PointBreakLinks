using Domain.Common;

namespace Domain.Entities;

// Audit trail for ApproveSiteCommandHandler/RejectSiteCommandHandler (including when triggered
// via ModerationController's bulk-approve/bulk-reject) — moderation decisions were previously
// untracked: a site's current StatusId shows the outcome but not who decided it or when.
public class ModerationAuditEntry : BaseEntity
{
    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;

    public int ModeratorId { get; set; }
    public User Moderator { get; set; } = null!;

    public string Action { get; set; } = string.Empty; // "approved" | "rejected"

    // Only ever set for "rejected" — lets the seller (and the audit log) see why, instead of a
    // bare status flip. Optional: a moderator isn't forced to type one.
    public string? Reason { get; set; }
}
