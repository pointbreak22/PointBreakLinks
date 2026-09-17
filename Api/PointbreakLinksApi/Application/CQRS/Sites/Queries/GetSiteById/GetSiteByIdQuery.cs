using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetSiteById;

public record GetSiteByIdQuery(int SiteId, int OwnerId) : IRequest<SiteDto>;
