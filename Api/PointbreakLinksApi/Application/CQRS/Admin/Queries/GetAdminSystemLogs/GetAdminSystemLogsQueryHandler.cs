using Application.Common;
using Application.CQRS.Admin.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetAdminSystemLogs;

public class GetAdminSystemLogsQueryHandler(IPurchasedSiteEventRepository eventRepository)
    : IRequestHandler<GetAdminSystemLogsQuery, PagedResult<AdminSystemLogDto>>
{
    public async Task<PagedResult<AdminSystemLogDto>> Handle(GetAdminSystemLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await eventRepository.GetAllForAdminAsync(request.Page, request.PerPage, cancellationToken);

        var dtos = items
            .Select(e => new AdminSystemLogDto(e.Id, e.PurchasedSiteId, e.SiteUrl, e.Description, e.CreatedAt.ToString("dd.MM.yyyy HH:mm")))
            .ToList();

        return PagedResult<AdminSystemLogDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
