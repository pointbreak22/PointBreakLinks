using Application.Common;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.DeactivateSite;

public class DeactivateSiteCommandHandler(ISiteRepository siteRepository, IDynamicStatsRefresher statsRefresher)
    : IRequestHandler<DeactivateSiteCommand>
{
    public async Task Handle(DeactivateSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdForOwnerAsync(request.SiteId, request.OwnerId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        site.IsActive = false;
        await siteRepository.SaveChangesAsync(cancellationToken);
        await statsRefresher.RefreshSiteCountsAsync(request.OwnerId, cancellationToken);
    }
}
