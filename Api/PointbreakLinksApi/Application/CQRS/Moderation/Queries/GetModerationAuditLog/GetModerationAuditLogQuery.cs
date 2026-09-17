using Application.Common;
using Application.CQRS.Moderation.DTOs;
using MediatR;

namespace Application.CQRS.Moderation.Queries.GetModerationAuditLog;

public record GetModerationAuditLogQuery(int Page, int PerPage) : IRequest<PagedResult<ModerationAuditEntryDto>>;
