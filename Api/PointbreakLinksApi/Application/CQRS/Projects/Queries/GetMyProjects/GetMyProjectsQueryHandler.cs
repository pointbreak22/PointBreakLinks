using Application.Common;
using Application.CQRS.Projects.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Projects.Queries.GetMyProjects;

public class GetMyProjectsQueryHandler(IProjectRepository projectRepository) : IRequestHandler<GetMyProjectsQuery, PagedResult<ProjectDto>>
{
    public async Task<PagedResult<ProjectDto>> Handle(GetMyProjectsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await projectRepository.GetByUserAsync(request.UserId, request.Page, request.PerPage, cancellationToken);
        return PagedResult<ProjectDto>.Create(items.Select(ProjectDto.FromEntity).ToList(), total, request.Page, request.PerPage);
    }
}
