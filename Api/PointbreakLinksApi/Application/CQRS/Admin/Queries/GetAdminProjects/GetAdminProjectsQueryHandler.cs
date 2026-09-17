using Application.Common;
using Application.CQRS.Admin.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetAdminProjects;

public class GetAdminProjectsQueryHandler(IProjectRepository projectRepository) : IRequestHandler<GetAdminProjectsQuery, PagedResult<AdminProjectDto>>
{
    public async Task<PagedResult<AdminProjectDto>> Handle(GetAdminProjectsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await projectRepository.GetAllForAdminAsync(request.Page, request.PerPage, cancellationToken);

        var dtos = items
            .Select(p => new AdminProjectDto(p.Id, p.Name, p.OwnerName, p.Type, p.TotalLinks, p.LinksPosted, p.SpentMoney, p.CreatedAt.ToString("dd.MM.yyyy")))
            .ToList();

        return PagedResult<AdminProjectDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
