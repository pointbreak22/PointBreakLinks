using Application.Common;
using Application.CQRS.Moderation.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Moderation.Queries.GetModerationAuditLog;

public class GetModerationAuditLogQueryHandler(IModerationAuditRepository auditRepository)
    : IRequestHandler<GetModerationAuditLogQuery, PagedResult<ModerationAuditEntryDto>>
{
    public async Task<PagedResult<ModerationAuditEntryDto>> Handle(GetModerationAuditLogQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await auditRepository.GetPagedAsync(request.Page, request.PerPage, cancellationToken);
        return PagedResult<ModerationAuditEntryDto>.Create(items.Select(ModerationAuditEntryDto.FromEntity).ToList(), total, request.Page, request.PerPage);
    }
}
