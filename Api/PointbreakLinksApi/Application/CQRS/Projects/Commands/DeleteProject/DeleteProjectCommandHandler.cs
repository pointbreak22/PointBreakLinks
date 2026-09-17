using Application.Common;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Projects.Commands.DeleteProject;

public class DeleteProjectCommandHandler(IProjectRepository projectRepository, IDynamicStatsRefresher statsRefresher)
    : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdForOwnerAsync(request.ProjectId, request.UserId, cancellationToken)
                      ?? throw new NotFoundException("Project", request.ProjectId);

        projectRepository.Remove(project);
        await projectRepository.SaveChangesAsync(cancellationToken);
        await statsRefresher.RefreshProjectCountAsync(cancellationToken);
    }
}
