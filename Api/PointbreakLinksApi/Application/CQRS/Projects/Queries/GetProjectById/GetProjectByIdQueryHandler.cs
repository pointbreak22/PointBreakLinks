using Application.CQRS.Projects.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Projects.Queries.GetProjectById;

public class GetProjectByIdQueryHandler(IProjectRepository projectRepository) : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdForOwnerAsync(request.ProjectId, request.UserId, cancellationToken)
                      ?? throw new NotFoundException("Project", request.ProjectId);
        return ProjectDto.FromEntity(project);
    }
}
