using Domain.Entities;
using Domain.Enums;

namespace Application.CQRS.Sites.DTOs;

public record LinkDto(string Query, string Url);

public record PaymentSettingDto(InsuranceType InsuranceType, bool CheckUniqueness, bool IsUrgent, bool IsExpertArticle);

public record BuyerDto(int Id, string Name);

public record LastMessageDto(string Text, string CreatedAt, bool IsUnread);

// Mirrors FOXLinks' PurchasedSiteResource. Site must have Country/Topic/Status loaded, and
// this entity must have Site/Status/Buyer/Links/PaymentSetting/Messages loaded (Include)
// before calling FromEntity.
public record PurchasedSiteDto(
    int Id,
    int ProjectId,
    SiteDto Site,
    decimal PriceFinal,
    StatusDto Status,
    string? TaskDescription,
    IReadOnlyList<LinkDto> Links,
    PaymentSettingDto? PaymentSettings,
    BuyerDto Buyer,
    bool FlHasLinks,
    bool IsPublished,
    string UpdatedAt,
    LastMessageDto? LastMessage,
    bool HasReview,
    bool IsDisputed)
{
    // currentUserId decides IsUnread (unread = not yet read AND not sent by the viewer) —
    // real, unlike FOXLinks' PurchasedSiteResource, which never actually populated is_unread
    // even though the frontend's TS interface and my-sales.vue's unread-dot both expect it.
    public static PurchasedSiteDto FromEntity(PurchasedSite order, int currentUserId)
    {
        var latest = order.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

        return new PurchasedSiteDto(
            order.Id,
            order.ProjectId,
            SiteDto.FromEntity(order.Site),
            order.FinalPrice,
            new StatusDto(order.Status.Name, order.Status.Description ?? string.Empty),
            order.TaskDescription,
            order.Links.Select(l => new LinkDto(l.Name, l.Url)).ToList(),
            order.PaymentSetting == null
                ? null
                : new PaymentSettingDto(order.PaymentSetting.InsuranceType, order.PaymentSetting.CheckUniqueness, order.PaymentSetting.IsUrgent, order.PaymentSetting.IsExpertArticle),
            new BuyerDto(order.BuyerId, order.Buyer?.Name ?? "Система"),
            order.HasLinks,
            order.IsPublished,
            order.UpdatedAt.ToString("dd.MM.yyyy"),
            latest == null
                ? null
                : new LastMessageDto(Truncate(latest.Text, 50), latest.CreatedAt.ToString("dd.MM.yyyy HH:mm"), latest.ReadAt == null && latest.SenderId != currentUserId),
            order.Review != null,
            order.IsDisputed);
    }

    private static string Truncate(string text, int length) =>
        text.Length > length ? text[..length] + "..." : text;
}
