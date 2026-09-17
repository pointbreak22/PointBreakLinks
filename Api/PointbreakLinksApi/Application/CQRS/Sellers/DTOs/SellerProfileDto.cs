using Application.CQRS.Sites.DTOs;
using Domain.Entities;

namespace Application.CQRS.Sellers.DTOs;

public record SellerProfileDto(
    int Id,
    string Name,
    string MemberSince,
    decimal? AverageRating,
    int ReviewsCount,
    IReadOnlyList<SiteDto> ActiveSites)
{
    // ActiveSites/AverageRating/ReviewsCount are assembled separately by the handler (they come
    // from ISiteRepository/ISiteReviewRepository, not from the User entity itself).
    public static SellerProfileDto FromEntity(User seller, decimal? averageRating, int reviewsCount, IReadOnlyList<SiteDto> activeSites) => new(
        seller.Id,
        seller.Name,
        seller.CreatedAt.ToString("dd.MM.yyyy"),
        averageRating,
        reviewsCount,
        activeSites);
}
