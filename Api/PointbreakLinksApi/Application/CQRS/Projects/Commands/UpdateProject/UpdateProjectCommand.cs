using Application.CQRS.Projects.DTOs;
using MediatR;

namespace Application.CQRS.Projects.Commands.UpdateProject;

public record UpdateProjectCommand(
    int ProjectId,
    int UserId,
    string Name,
    string? Url,
    string TaskForVm,
    bool FlLossInsurance,
    bool FlLossAndIndexationInsurance) : IRequest<ProjectDto>;
