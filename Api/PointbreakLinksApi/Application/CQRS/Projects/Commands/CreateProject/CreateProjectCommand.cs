using Application.CQRS.Projects.DTOs;
using MediatR;

namespace Application.CQRS.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    int UserId,
    string Name,
    string? Url,
    string TaskForVm,
    bool FlLossInsurance,
    bool FlLossAndIndexationInsurance) : IRequest<ProjectDto>;
