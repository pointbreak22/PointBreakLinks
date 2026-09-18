using Application.CQRS.Reviews.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Reviews.Commands.CreateReview;

// New feature, not from FOXLinks. Eligibility is real, not just "any buyer, any site": the
// order must belong to the caller and the seller must have already confirmed publication
// (IsPublished — see ConfirmPublishedCommandHandler) — a review only makes sense once the
// placement actually happened. One review per order (SiteReviewConfiguration's unique index
// on PurchasedSiteId), not one per buyer per site, so a buyer can't leave five reviews for
// five links on the same site in one sitting.
public class CreateReviewCommandHandler(
    IPurchasedSiteRepository purchasedSiteRepository,
    ISiteReviewRepository siteReviewRepository,
    IPurchasedSiteEventRepository purchasedSiteEventRepository)
    : IRequestHandler<CreateReviewCommand, SiteReviewDto>
{
    public async Task<SiteReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        // Rating 1-5 enforced by CreateReviewCommandValidator.
        var order = await purchasedSiteRepository.GetByIdForBuyerAsync(request.PurchasedSiteId, request.BuyerId, cancellationToken)
                    ?? throw new NotFoundException("PurchasedSite", request.PurchasedSiteId);

        if (!order.IsPublished)
        {
            throw new ForbiddenException("Оставить отзыв можно только после публикации размещения.");
        }

        if (await siteReviewRepository.ExistsForPurchasedSiteAsync(request.PurchasedSiteId, cancellationToken))
        {
            throw new ConflictException("Отзыв для этого заказа уже оставлен.");
        }

        var review = new SiteReview
        {
            SiteId = order.SiteId,
            BuyerId = request.BuyerId,
            PurchasedSiteId = request.PurchasedSiteId,
            Rating = request.Rating,
            Comment = request.Comment,
        };

        await siteReviewRepository.AddAsync(review, cancellationToken);
        await siteReviewRepository.SaveChangesAsync(cancellationToken);

        await purchasedSiteEventRepository.AddAsync(
            new PurchasedSiteEvent { PurchasedSiteId = request.PurchasedSiteId, Description = "Покупатель оставил отзыв" },
            cancellationToken);
        await purchasedSiteEventRepository.SaveChangesAsync(cancellationToken);

        review.Buyer = order.Buyer;
        return SiteReviewDto.FromEntity(review);
    }
}
