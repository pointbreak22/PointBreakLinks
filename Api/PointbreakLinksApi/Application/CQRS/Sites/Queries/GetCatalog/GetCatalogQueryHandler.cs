using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetCatalog;

public class GetCatalogQueryHandler(ISiteRepository siteRepository) : IRequestHandler<GetCatalogQuery, PagedResult<SiteDto>>
{
    public async Task<PagedResult<SiteDto>> Handle(GetCatalogQuery request, CancellationToken cancellationToken)
    {
        var filter = new SiteCatalogFilter(
            request.TopicId, request.CountryId, request.MinPrice, request.MaxPrice, request.MinIks, request.MinDr,
            request.SortBy, request.SortDescending);
        var (items, total) = await siteRepository.GetCatalogAsync(request.Page, request.PerPage, filter, cancellationToken);
        return PagedResult<SiteDto>.Create(items.Select(SiteDto.FromEntity).ToList(), total, request.Page, request.PerPage);
    }
}
