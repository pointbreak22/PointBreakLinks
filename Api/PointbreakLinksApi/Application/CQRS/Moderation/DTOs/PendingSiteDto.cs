using Domain.Entities;

namespace Application.CQRS.Moderation.DTOs;

// Staff-facing shape, deliberately separate from Sites/DTOs/SiteDto (that one mirrors FOXLinks'
// buyer/seller-facing SiteResource 1:1 and has no seller name) — the moderation queue needs to
// show who submitted a listing, which isn't a buyer/seller concern.
public record PendingSiteDto(
    int Id,
    string Url,
    string Topic,
    decimal Price,
    int Iks,
    byte Dr,
    string SellerName,
    string CreatedAt)
{
    // Requires Topic and Seller loaded (see ISiteRepository.GetPendingModerationAsync).
    public static PendingSiteDto FromEntity(Site site) => new(
        site.Id,
        site.Url,
        site.Topic.Name,
        site.Price,
        site.Iks,
        site.Dr,
        site.Seller.Name,
        site.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
}
