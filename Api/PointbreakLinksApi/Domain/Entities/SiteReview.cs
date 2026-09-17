using Domain.Common;

namespace Domain.Entities;

// New entity, not from FOXLinks. One review per completed order (PurchasedSiteId is unique —
// see SiteReviewConfiguration) rather than one per buyer per site, so a review is always tied
// to a real placement that actually happened, not just an opinion anyone could post.
public class SiteReview : BaseEntity
{
    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;

    public int BuyerId { get; set; }
    public User Buyer { get; set; } = null!;

    public int PurchasedSiteId { get; set; }
    public PurchasedSite PurchasedSite { get; set; } = null!;

    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }

    // One reply per review, from the site's own seller — see ReplyToReviewCommandHandler.
    public string? SellerReply { get; set; }
    public DateTime? SellerRepliedAt { get; set; }
}
