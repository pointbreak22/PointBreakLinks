using Application.Common;
using Application.CQRS.Sites.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetProjectSites;

public class GetProjectSitesQueryHandler(IProjectRepository projectRepository, IPurchasedSiteRepository purchasedSiteRepository)
    : IRequestHandler<GetProjectSitesQuery, PagedResult<PurchasedSiteDto>>
{
    public async Task<PagedResult<PurchasedSiteDto>> Handle(GetProjectSitesQuery request, CancellationToken cancellationToken)
    {
        // Confirms the project actually belongs to the caller — see IPurchasedSiteRepository's
        // GetByProjectAsync comment for why this check exists here and not in FOXLinks.
        _ = await projectRepository.GetByIdForOwnerAsync(request.ProjectId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var (items, total) = await purchasedSiteRepository.GetByProjectAsync(request.ProjectId, request.Page, request.PerPage, cancellationToken);
        var dtos = items.Select(o => PurchasedSiteDto.FromEntity(o, request.UserId)).ToList();
        return PagedResult<PurchasedSiteDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
