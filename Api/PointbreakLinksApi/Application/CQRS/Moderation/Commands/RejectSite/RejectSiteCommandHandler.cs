using Application.Common;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Moderation.Commands.RejectSite;

public class RejectSiteCommandHandler(
    ISiteRepository siteRepository,
    INotificationPusher notificationPusher,
    INotificationRepository notificationRepository,
    IModerationAuditRepository moderationAuditRepository,
    ILogger<RejectSiteCommandHandler> logger) : IRequestHandler<RejectSiteCommand>
{
    // Matches the fixed id StatusConfiguration.HasData seeds for "rejected".
    private const int RejectedStatusId = 3;

    public async Task Handle(RejectSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdAsync(request.SiteId, cancellationToken)
                   ?? throw new NotFoundException("Site", request.SiteId);

        site.StatusId = RejectedStatusId;
        site.IsActive = false;
        await moderationAuditRepository.AddAsync(
            new ModerationAuditEntry { SiteId = site.Id, ModeratorId = request.ModeratorId, Action = "rejected", Reason = request.Reason },
            cancellationToken);
        await siteRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Site {SiteId} ({Url}) rejected by moderator {ModeratorId}. Reason: {Reason}",
            site.Id, site.Url, request.ModeratorId, request.Reason ?? "(not specified)");

        await notificationPusher.NotifySiteModeratedAsync(site.SellerId, site.Url, approved: false, cancellationToken);

        var message = string.IsNullOrWhiteSpace(request.Reason)
            ? $"Площадка {site.Url} отклонена"
            : $"Площадка {site.Url} отклонена. Причина: {request.Reason}";
        await notificationRepository.AddAsync(new Notification { UserId = site.SellerId, Message = message }, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);
    }
}
