using Domain.Common;

namespace Domain.Entities;

// A single anchor/URL pair the buyer wants posted on a purchased placement.
public class Link : BaseEntity
{
    public int PurchasedSiteId { get; set; }
    public PurchasedSite PurchasedSite { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // anchor text
}
