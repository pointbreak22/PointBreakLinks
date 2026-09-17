using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetMySites;

public class GetMySitesQueryHandler(ISiteRepository siteRepository) : IRequestHandler<GetMySitesQuery, PagedResult<SiteDto>>
{
    public async Task<PagedResult<SiteDto>> Handle(GetMySitesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await siteRepository.GetByUserAsync(request.UserId, request.Page, request.PerPage, cancellationToken);
        return PagedResult<SiteDto>.Create(items.Select(SiteDto.FromEntity).ToList(), total, request.Page, request.PerPage);
    }
}
