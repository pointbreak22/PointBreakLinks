using Application.CQRS.Moderation.Commands.RejectSite;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Moderation;

public class RejectSiteCommandHandlerTests
{
    [Fact]
    public async Task Handle_Rejection_DeactivatesSiteAndRecordsReason()
    {
        var site = TestBuilders.Site();
        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.GetByIdAsync(site.Id, It.IsAny<CancellationToken>())).ReturnsAsync(site);

        var auditRepo = new Mock<IModerationAuditRepository>();
        ModerationAuditEntry? capturedEntry = null;
        auditRepo.Setup(r => r.AddAsync(It.IsAny<ModerationAuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<ModerationAuditEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .Returns(Task.CompletedTask);

        var handler = new RejectSiteCommandHandler(
            siteRepo.Object, Mock.Of<Application.Common.INotificationPusher>(), Mock.Of<INotificationRepository>(), auditRepo.Object,
            Mock.Of<ILogger<RejectSiteCommandHandler>>());

        await handler.Handle(new RejectSiteCommand(site.Id, ModeratorId: 5, Reason: "Некачественный контент"), CancellationToken.None);

        Assert.Equal(3, site.StatusId); // "rejected"
        Assert.False(site.IsActive);
        Assert.NotNull(capturedEntry);
        Assert.Equal("rejected", capturedEntry!.Action);
        Assert.Equal("Некачественный контент", capturedEntry.Reason);
    }

    [Fact]
    public async Task Handle_RejectionWithoutReason_NotificationOmitsReasonClause()
    {
        var site = TestBuilders.Site();
        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.GetByIdAsync(site.Id, It.IsAny<CancellationToken>())).ReturnsAsync(site);

        var notificationRepo = new Mock<INotificationRepository>();
        Notification? captured = null;
        notificationRepo.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);

        var handler = new RejectSiteCommandHandler(
            siteRepo.Object, Mock.Of<Application.Common.INotificationPusher>(), notificationRepo.Object, Mock.Of<IModerationAuditRepository>(),
            Mock.Of<ILogger<RejectSiteCommandHandler>>());

        await handler.Handle(new RejectSiteCommand(site.Id, ModeratorId: 5, Reason: null), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.DoesNotContain("Причина", captured!.Message);
    }
}
