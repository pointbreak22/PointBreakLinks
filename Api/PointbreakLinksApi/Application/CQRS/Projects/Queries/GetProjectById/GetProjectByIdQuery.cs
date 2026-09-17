using Application.CQRS.Projects.DTOs;
using MediatR;

namespace Application.CQRS.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(int ProjectId, int UserId) : IRequest<ProjectDto>;
