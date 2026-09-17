using Domain.Common;

namespace Domain.Entities;

// New entity, not from FOXLinks — a real timeline of what happened to an order (application
// created, accepted, published, reviewed), using BaseEntity.CreatedAt as the event timestamp
// rather than duplicating a separate field. Not every entry is a StatusId change (publication
// confirmation doesn't change PurchasedSite.StatusId at all) — this records anything worth
// showing on a timeline, not strictly a status-transition log.
public class PurchasedSiteEvent : BaseEntity
{
    public int PurchasedSiteId { get; set; }
    public PurchasedSite PurchasedSite { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
}
