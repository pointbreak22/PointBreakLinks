using Domain.Common;

namespace Domain.Entities;

// Shared lookup table reused by both Site (moderation status) and PurchasedSite (order
// status) — that's how FOXLinks modeled it (one `statuses` table, two unrelated foreign
// keys into it). Kept as-is for parity; worth revisiting as two separate enums later.
public class Status : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
