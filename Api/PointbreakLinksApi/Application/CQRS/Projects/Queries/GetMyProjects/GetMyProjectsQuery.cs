using Application.Common;
using Application.CQRS.Projects.DTOs;
using MediatR;

namespace Application.CQRS.Projects.Queries.GetMyProjects;

public record GetMyProjectsQuery(int UserId, int Page, int PerPage) : IRequest<PagedResult<ProjectDto>>;
