using Application.Common;
using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetProjectSites;

public record GetProjectSitesQuery(int ProjectId, int UserId, int Page, int PerPage) : IRequest<PagedResult<PurchasedSiteDto>>;
