namespace Application.CQRS.Admin.DTOs;

public record AdminSystemLogDto(int Id, int PurchasedSiteId, string SiteUrl, string Description, string CreatedAt);
