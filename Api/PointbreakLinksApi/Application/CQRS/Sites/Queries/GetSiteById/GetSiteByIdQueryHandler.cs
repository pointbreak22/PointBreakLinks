using Application.CQRS.Sites.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetSiteById;

public class GetSiteByIdQueryHandler(ISiteRepository siteRepository) : IRequestHandler<GetSiteByIdQuery, SiteDto>
{
    public async Task<SiteDto> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdForOwnerAsync(request.SiteId, request.OwnerId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);
        return SiteDto.FromEntity(site);
    }
}
