using Application.Common;
using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetMySites;

public record GetMySitesQuery(int UserId, int Page, int PerPage) : IRequest<PagedResult<SiteDto>>;
