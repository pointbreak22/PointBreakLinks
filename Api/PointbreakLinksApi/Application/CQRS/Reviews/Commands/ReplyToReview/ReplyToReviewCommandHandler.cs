using Application.CQRS.Reviews.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Reviews.Commands.ReplyToReview;

// One reply per review — a seller editing an existing reply just overwrites it (SellerRepliedAt
// moves forward too), there's no separate "edit" endpoint since that would just be this again.
public class ReplyToReviewCommandHandler(ISiteReviewRepository siteReviewRepository)
    : IRequestHandler<ReplyToReviewCommand, SiteReviewDto>
{
    public async Task<SiteReviewDto> Handle(ReplyToReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await siteReviewRepository.GetByIdAsync(request.ReviewId, cancellationToken)
                     ?? throw new NotFoundException("SiteReview", request.ReviewId);

        if (review.Site.SellerId != request.SellerId)
        {
            throw new ForbiddenException("Это не ваша площадка.");
        }

        // Reply-not-empty enforced by ReplyToReviewCommandValidator.
        review.SellerReply = request.Reply;
        review.SellerRepliedAt = DateTime.UtcNow;
        await siteReviewRepository.SaveChangesAsync(cancellationToken);

        return SiteReviewDto.FromEntity(review);
    }
}
