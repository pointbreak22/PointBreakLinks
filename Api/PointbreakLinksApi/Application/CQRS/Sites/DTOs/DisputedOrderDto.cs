namespace Application.CQRS.Sites.DTOs;

public record DisputedOrderDto(int Id, string SiteUrl, string BuyerName, string SellerName, decimal FinalPrice, string Reason, string UpdatedAt);
