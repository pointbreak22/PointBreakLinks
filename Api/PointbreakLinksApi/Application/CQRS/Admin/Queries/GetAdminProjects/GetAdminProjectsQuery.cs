using Application.Common;
using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetAdminProjects;

public record GetAdminProjectsQuery(int Page, int PerPage) : IRequest<PagedResult<AdminProjectDto>>;
