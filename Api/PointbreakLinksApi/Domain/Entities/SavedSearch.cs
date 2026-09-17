using Domain.Common;

namespace Domain.Entities;

// New, not from FOXLinks — optimizator.vue's filter panels were entirely decorative (see
// ISiteRepository's SiteCatalogFilter comment: this app's real filter set was built from
// scratch). Each row is one buyer's alert criteria, every field optional ("any"), matched the
// same way SiteCatalogFilter is applied to the catalog query itself — see
// ISavedSearchRepository.GetMatchingAsync. Checked once, at the moment a site actually enters
// the catalog for the first time (ApproveSiteCommandHandler sets IsActive true) rather than on
// every catalog read, so a buyer only ever gets notified once per newly approved listing.
public class SavedSearch : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public int? CountryId { get; set; }
    public Country? Country { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinIks { get; set; }
    public int? MinDr { get; set; }
}
