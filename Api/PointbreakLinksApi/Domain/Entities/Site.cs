using Domain.Common;

namespace Domain.Entities;

// A donor site/platform listed in the catalog for sale (площадка).
public class Site : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int? CountryId { get; set; }
    public Country? Country { get; set; }

    public int TopicId { get; set; }
    public Topic Topic { get; set; } = null!;

    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;

    public int SellerId { get; set; } // user_id_sales
    public User Seller { get; set; } = null!;

    public decimal Price { get; set; }
    public int Iks { get; set; }
    public byte Dr { get; set; }
    public int Traffic { get; set; }
    public int SoldCount { get; set; }
    public bool IsActive { get; set; } = true;

    // Ownership verification — new, not from FOXLinks. A seller proves control of the listed
    // domain by publishing this token (meta tag or a well-known text file); see
    // Application/Common/ISiteVerificationService.cs for how it's checked.
    public string VerificationToken { get; set; } = string.Empty;
    public bool IsVerified { get; set; }

    // Relative path under wwwroot (e.g. "uploads/sites/{guid}.jpg") — null until the seller
    // uploads one. Storage/serving lives entirely in WebAPI (SitesController), same split as
    // Message.AttachmentPath; this column is just the pointer.
    public string? ScreenshotPath { get; set; }

    public ICollection<PurchasedSite> PurchasedSites { get; set; } = [];
    public ICollection<SiteReview> Reviews { get; set; } = [];
}
