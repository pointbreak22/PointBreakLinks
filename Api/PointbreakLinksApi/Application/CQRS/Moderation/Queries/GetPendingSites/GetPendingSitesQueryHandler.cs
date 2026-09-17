using Application.Common;
using Application.CQRS.Moderation.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Moderation.Queries.GetPendingSites;

public class GetPendingSitesQueryHandler(ISiteRepository siteRepository) : IRequestHandler<GetPendingSitesQuery, PagedResult<PendingSiteDto>>
{
    public async Task<PagedResult<PendingSiteDto>> Handle(GetPendingSitesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await siteRepository.GetPendingModerationAsync(request.Page, request.PerPage, cancellationToken);
        return PagedResult<PendingSiteDto>.Create(items.Select(PendingSiteDto.FromEntity).ToList(), total, request.Page, request.PerPage);
    }
}
