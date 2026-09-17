using Application.CQRS.Projects.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler(IProjectRepository projectRepository) : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdForOwnerAsync(request.ProjectId, request.UserId, cancellationToken)
                      ?? throw new NotFoundException("Project", request.ProjectId);

        project.Name = request.Name;
        project.Url = request.Url;
        project.TaskForVm = request.TaskForVm;
        project.HasLossInsurance = request.FlLossInsurance;
        project.HasLossAndIndexationInsurance = request.FlLossAndIndexationInsurance;

        await projectRepository.SaveChangesAsync(cancellationToken);

        return ProjectDto.FromEntity(project);
    }
}
