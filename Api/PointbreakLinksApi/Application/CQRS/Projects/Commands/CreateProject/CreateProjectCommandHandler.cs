using Application.Common;
using Application.CQRS.Projects.DTOs;
using Domain.Entities;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler(IProjectRepository projectRepository, IDynamicStatsRefresher statsRefresher)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            UserId = request.UserId,
            Name = request.Name,
            // "folder" is the only project type FOXLinks' create-edit-project-modal.vue ever
            // sends — StoreProjectRequest validates `type` as a free string, but nothing in the
            // UI lets it be anything else, so it isn't exposed as a client-controlled field here.
            Type = "folder",
            Url = request.Url,
            TaskForVm = request.TaskForVm,
            HasLossInsurance = request.FlLossInsurance,
            HasLossAndIndexationInsurance = request.FlLossAndIndexationInsurance,
        };

        await projectRepository.AddAsync(project, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);
        await statsRefresher.RefreshProjectCountAsync(cancellationToken);

        return ProjectDto.FromEntity(project);
    }
}
