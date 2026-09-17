using Domain.Entities;

namespace Application.CQRS.Reviews.DTOs;

public record SiteReviewDto(
    int Id,
    string BuyerName,
    int Rating,
    string? Comment,
    string CreatedAt,
    string? SellerReply,
    string? SellerRepliedAt)
{
    // Requires Buyer loaded (see ISiteReviewRepository.GetBySiteAsync).
    public static SiteReviewDto FromEntity(SiteReview review) => new(
        review.Id,
        review.Buyer.Name,
        review.Rating,
        review.Comment,
        review.CreatedAt.ToString("dd.MM.yyyy"),
        review.SellerReply,
        review.SellerRepliedAt?.ToString("dd.MM.yyyy"));
}
