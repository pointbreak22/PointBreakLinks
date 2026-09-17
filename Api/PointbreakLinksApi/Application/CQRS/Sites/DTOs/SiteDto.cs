using Domain.Entities;

namespace Application.CQRS.Sites.DTOs;

public record CountryDto(int Id, string Code, string Name);

public record StatusDto(string Name, string Description);

public record TopicDto(int Id, string Name);

// Field names/shape mirror FOXLinks' SiteResource exactly, so the Angular DTOs ported from
// interfaces/site.ts need no changes.
public record SiteDto(
    int Id,
    string Url,
    int Iks,
    int Dr,
    int Traffic,
    CountryDto? Country,
    int? CountryId,
    decimal Price,
    string? Description,
    string Topic,
    int TopicId,
    StatusDto Status,
    int UserIdSales,
    string SellerName,
    int SoldCount,
    string UpdatedAt,
    bool IsVerified,
    decimal? AverageRating,
    int ReviewsCount,
    string VerificationToken,
    // Only meaningful to the owner (pause/resume via DeactivateSite/ReactivateSiteCommand) — the
    // catalog never returns a false one anyway, since GetCatalogAsync filters on it.
    bool IsActive,
    // A scalar column (no Include needed), so this is safe on every SiteDto.FromEntity call
    // site unlike SellerName above. The actual file is served by SitesController's own
    // [AllowAnonymous] GET /sites/{id}/screenshot — this is just "does one exist".
    bool HasScreenshot)
{
    // VerificationToken is included on every SiteDto (catalog included), not just for the
    // owner — it's not a secret in the usual sense, its entire purpose is to be published
    // publicly on the seller's own site as proof of control, so there's nothing to protect by
    // scoping it to a separate owner-only DTO.
    // Site must have Country/Topic/Status/Reviews loaded (Include) before calling this.
    // Seller is loaded by ISiteRepository's own IncludeAll() (catalog, my-sites, get-by-id) but
    // NOT by every PurchasedSite-side query that embeds a Site (accept/decline/cancel/dispute
    // etc.) — SellerName degrades to "" there rather than NullReferenceException-ing dozens of
    // call sites that have never needed it and don't render it.
    public static SiteDto FromEntity(Site site) => new(
        site.Id,
        site.Url,
        site.Iks,
        site.Dr,
        site.Traffic,
        site.Country == null ? null : new CountryDto(site.Country.Id, site.Country.Code, site.Country.Name),
        site.CountryId,
        site.Price,
        site.Description,
        site.Topic.Name,
        site.TopicId,
        new StatusDto(site.Status.Name, site.Status.Description ?? string.Empty),
        site.SellerId,
        site.Seller?.Name ?? string.Empty,
        site.SoldCount,
        site.UpdatedAt.ToString("dd.MM.yyyy HH:mm"),
        site.IsVerified,
        site.Reviews.Count == 0 ? null : Math.Round((decimal)site.Reviews.Average(r => r.Rating), 1),
        site.Reviews.Count,
        site.VerificationToken,
        site.IsActive,
        !string.IsNullOrEmpty(site.ScreenshotPath));
}
