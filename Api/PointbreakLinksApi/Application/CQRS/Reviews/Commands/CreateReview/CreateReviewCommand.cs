using Application.CQRS.Reviews.DTOs;
using MediatR;

namespace Application.CQRS.Reviews.Commands.CreateReview;

public record CreateReviewCommand(int PurchasedSiteId, int BuyerId, int Rating, string? Comment) : IRequest<SiteReviewDto>;
