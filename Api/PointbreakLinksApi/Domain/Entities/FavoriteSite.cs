using Domain.Common;

namespace Domain.Entities;

// New entity, not from FOXLinks — the source's "Избранные площадки" links (both on the
// webmaster and optimizator sidebars) all pointed at /coming-soon; the feature was never built.
public class FavoriteSite : BaseEntity
{
    public int BuyerId { get; set; }
    public User Buyer { get; set; } = null!;

    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;
}
