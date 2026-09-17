using Application.Common;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Moderation.Commands.ApproveSite;

public class ApproveSiteCommandHandler(
    ISiteRepository siteRepository,
    ISavedSearchRepository savedSearchRepository,
    IDynamicStatsRefresher statsRefresher,
    INotificationPusher notificationPusher,
    INotificationRepository notificationRepository,
    IModerationAuditRepository moderationAuditRepository)
    : IRequestHandler<ApproveSiteCommand>
{
    // Matches the fixed id StatusConfiguration.HasData seeds for "active" — see
    // Domain/Constants/StatusNames.cs.
    private const int ActiveStatusId = 6;

    public async Task Handle(ApproveSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdAsync(request.SiteId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        site.StatusId = ActiveStatusId;
        site.IsActive = true;
        await moderationAuditRepository.AddAsync(
            new ModerationAuditEntry { SiteId = site.Id, ModeratorId = request.ModeratorId, Action = "approved" },
            cancellationToken);
        await siteRepository.SaveChangesAsync(cancellationToken);
        await statsRefresher.RefreshSiteCountsAsync(site.SellerId, cancellationToken);
        await notificationPusher.NotifySiteModeratedAsync(site.SellerId, site.Url, approved: true, cancellationToken);

        await notificationRepository.AddAsync(
            new Notification { UserId = site.SellerId, Message = $"Площадка {site.Url} одобрена" },
            cancellationToken);

        // This is the one moment a listing actually enters the catalog (GetCatalogAsync filters
        // on IsActive) — alert every buyer whose saved-search criteria it now satisfies, once
        // per buyer even if several of their saved searches all match.
        var matchingSearches = await savedSearchRepository.GetMatchingAsync(site, cancellationToken);
        foreach (var userId in matchingSearches.Select(s => s.UserId).Distinct())
        {
            await notificationRepository.AddAsync(
                new Notification { UserId = userId, Message = $"Новая площадка по сохранённому поиску: {site.Url}" },
                cancellationToken);
        }

        await notificationRepository.SaveChangesAsync(cancellationToken);
    }
}
