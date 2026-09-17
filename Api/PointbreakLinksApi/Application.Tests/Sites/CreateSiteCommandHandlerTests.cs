using Application.Common;
using Application.CQRS.Sites.Commands.CreateSite;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Moq;

namespace Application.Tests.Sites;

// Regression coverage for a real bug this app fixed relative to FOXLinks (see PROJECT_MAP.md's
// "Известные ограничения"): a brand-new listing must start unreviewed — IsActive=false,
// StatusId=2 ("На модерации") — never immediately live in the buyer-facing catalog.
public class CreateSiteCommandHandlerTests
{
    private static CreateSiteCommand Command(int sellerId = 10) => new(
        Url: "new-site.ru",
        TopicId: 1,
        Description: null,
        Price: 500m,
        Iks: 10,
        Dr: 20,
        Traffic: 100,
        CountryId: null,
        SellerId: sellerId);

    [Fact]
    public async Task Handle_DuplicateUrl_ThrowsConflict()
    {
        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.UrlExistsAsync("new-site.ru", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateSiteCommandHandler(siteRepo.Object, Mock.Of<IDynamicStatsRefresher>());

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(Command(), CancellationToken.None));

        siteRepo.Verify(r => r.AddAsync(It.IsAny<Site>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewSite_StartsUnapprovedAndInactive()
    {
        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.UrlExistsAsync("new-site.ru", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Site? captured = null;
        siteRepo.Setup(r => r.AddAsync(It.IsAny<Site>(), It.IsAny<CancellationToken>()))
            .Callback<Site, CancellationToken>((site, _) =>
            {
                // Simulates what a real repository's post-insert re-fetch would load: the
                // handler itself never sets Topic/Status navigations, only TopicId/StatusId.
                site.Topic = new Topic { Id = site.TopicId, Name = "Тема" };
                site.Status = new Status { Id = site.StatusId, Name = "moderation", Description = "На модерации" };
                captured = site;
            })
            .Returns(Task.CompletedTask);
        siteRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => captured);

        var handler = new CreateSiteCommandHandler(siteRepo.Object, Mock.Of<IDynamicStatsRefresher>());

        await handler.Handle(Command(), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.False(captured!.IsActive);
        Assert.Equal(2, captured.StatusId);
        Assert.NotEmpty(captured.VerificationToken);
    }
}
