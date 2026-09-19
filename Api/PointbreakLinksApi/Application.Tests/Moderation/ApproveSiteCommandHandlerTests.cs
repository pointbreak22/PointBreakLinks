using Application.Common;
using Application.CQRS.Moderation.Commands.ApproveSite;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Moderation;

public class ApproveSiteCommandHandlerTests
{
    [Fact]
    public async Task Handle_UnknownSite_ThrowsNotFound()
    {
        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Site?)null);
        var handler = new ApproveSiteCommandHandler(
            siteRepo.Object, Mock.Of<ISavedSearchRepository>(), Mock.Of<IDynamicStatsRefresher>(),
            Mock.Of<INotificationPusher>(), Mock.Of<INotificationRepository>(), Mock.Of<IModerationAuditRepository>(),
            Mock.Of<ILogger<ApproveSiteCommandHandler>>());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new ApproveSiteCommand(99, ModeratorId: 5), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PendingSite_BecomesActiveAndAudited()
    {
        var site = TestBuilders.Site();
        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.GetByIdAsync(site.Id, It.IsAny<CancellationToken>())).ReturnsAsync(site);

        var auditRepo = new Mock<IModerationAuditRepository>();
        ModerationAuditEntry? capturedEntry = null;
        auditRepo.Setup(r => r.AddAsync(It.IsAny<ModerationAuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<ModerationAuditEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .Returns(Task.CompletedTask);

        var savedSearchRepo = new Mock<ISavedSearchRepository>();
        savedSearchRepo.Setup(r => r.GetMatchingAsync(site, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SavedSearch>());

        var handler = new ApproveSiteCommandHandler(
            siteRepo.Object, savedSearchRepo.Object, Mock.Of<IDynamicStatsRefresher>(),
            Mock.Of<INotificationPusher>(), Mock.Of<INotificationRepository>(), auditRepo.Object,
            Mock.Of<ILogger<ApproveSiteCommandHandler>>());

        await handler.Handle(new ApproveSiteCommand(site.Id, ModeratorId: 5), CancellationToken.None);

        // StatusConfiguration seeds "active" as id 6 — a new listing only ever enters the
        // buyer-facing catalog (GetCatalogAsync filters on IsActive) via this exact transition.
        Assert.Equal(6, site.StatusId);
        Assert.True(site.IsActive);
        Assert.NotNull(capturedEntry);
        Assert.Equal("approved", capturedEntry!.Action);
        Assert.Equal(5, capturedEntry.ModeratorId);
    }

    [Fact]
    public async Task Handle_ApprovedSite_NotifiesEveryMatchingSavedSearchOncePerBuyer()
    {
        var site = TestBuilders.Site();
        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.GetByIdAsync(site.Id, It.IsAny<CancellationToken>())).ReturnsAsync(site);

        var savedSearchRepo = new Mock<ISavedSearchRepository>();
        savedSearchRepo.Setup(r => r.GetMatchingAsync(site, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SavedSearch>
            {
                new() { Id = 1, UserId = 100 },
                new() { Id = 2, UserId = 100 }, // same buyer, two matching saved searches
                new() { Id = 3, UserId = 200 },
            });

        var notificationRepo = new Mock<INotificationRepository>();
        var added = new List<Notification>();
        notificationRepo.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => added.Add(n))
            .Returns(Task.CompletedTask);

        var handler = new ApproveSiteCommandHandler(
            siteRepo.Object, savedSearchRepo.Object, Mock.Of<IDynamicStatsRefresher>(),
            Mock.Of<INotificationPusher>(), notificationRepo.Object, Mock.Of<IModerationAuditRepository>(),
            Mock.Of<ILogger<ApproveSiteCommandHandler>>());

        await handler.Handle(new ApproveSiteCommand(site.Id, ModeratorId: 5), CancellationToken.None);

        // One notification to the seller + exactly one per distinct matching buyer (100 once,
        // not twice, despite two matching saved searches) + one for buyer 200.
        Assert.Equal(3, added.Count);
        Assert.Equal(1, added.Count(n => n.UserId == 100));
        Assert.Equal(1, added.Count(n => n.UserId == 200));
    }
}
