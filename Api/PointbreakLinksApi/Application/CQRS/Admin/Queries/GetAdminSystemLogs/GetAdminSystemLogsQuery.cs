using Application.Common;
using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetAdminSystemLogs;

public record GetAdminSystemLogsQuery(int Page, int PerPage) : IRequest<PagedResult<AdminSystemLogDto>>;
