using Application.Common;
using Application.CQRS.Reviews.DTOs;
using MediatR;

namespace Application.CQRS.Reviews.Queries.GetSiteReviews;

public record GetSiteReviewsQuery(int SiteId, int Page, int PerPage) : IRequest<PagedResult<SiteReviewDto>>;
