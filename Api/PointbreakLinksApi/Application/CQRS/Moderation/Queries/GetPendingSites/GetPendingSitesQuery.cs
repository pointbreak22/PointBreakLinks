using Application.Common;
using Application.CQRS.Moderation.DTOs;
using MediatR;

namespace Application.CQRS.Moderation.Queries.GetPendingSites;

public record GetPendingSitesQuery(int Page, int PerPage) : IRequest<PagedResult<PendingSiteDto>>;
