using Application.Common;
using Application.CQRS.Reviews.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Reviews.Queries.GetSiteReviews;

public class GetSiteReviewsQueryHandler(ISiteReviewRepository siteReviewRepository) : IRequestHandler<GetSiteReviewsQuery, PagedResult<SiteReviewDto>>
{
    public async Task<PagedResult<SiteReviewDto>> Handle(GetSiteReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await siteReviewRepository.GetBySiteAsync(request.SiteId, request.Page, request.PerPage, cancellationToken);
        return PagedResult<SiteReviewDto>.Create(items.Select(SiteReviewDto.FromEntity).ToList(), total, request.Page, request.PerPage);
    }
}
