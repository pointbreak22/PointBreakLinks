using Application.Common;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Commands.ReactivateSite;

public class ReactivateSiteCommandHandler(ISiteRepository siteRepository, IDynamicStatsRefresher statsRefresher)
    : IRequestHandler<ReactivateSiteCommand>
{
    // Matches the fixed id StatusConfiguration.HasData seeds for "active" — see
    // ApproveSiteCommandHandler / Domain/Constants/StatusNames.cs.
    private const int ActiveStatusId = 6;

    public async Task Handle(ReactivateSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdForOwnerAsync(request.SiteId, request.OwnerId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        if (site.StatusId != ActiveStatusId)
        {
            throw new ConflictException("Возобновить можно только площадку, прошедшую модерацию.");
        }

        site.IsActive = true;
        await siteRepository.SaveChangesAsync(cancellationToken);
        await statsRefresher.RefreshSiteCountsAsync(request.OwnerId, cancellationToken);
    }
}
