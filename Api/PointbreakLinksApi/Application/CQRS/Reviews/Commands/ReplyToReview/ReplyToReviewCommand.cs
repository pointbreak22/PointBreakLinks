using Application.CQRS.Reviews.DTOs;
using MediatR;

namespace Application.CQRS.Reviews.Commands.ReplyToReview;

public record ReplyToReviewCommand(int ReviewId, int SellerId, string Reply) : IRequest<SiteReviewDto>;
