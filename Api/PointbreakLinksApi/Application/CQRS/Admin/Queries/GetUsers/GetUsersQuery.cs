using Application.Common;
using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetUsers;

public record GetUsersQuery(int Page, int PerPage, string? Search = null, string? Role = null) : IRequest<PagedResult<AdminUserDto>>;
