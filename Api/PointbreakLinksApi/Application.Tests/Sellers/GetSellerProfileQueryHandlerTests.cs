using Application.CQRS.Sellers.Queries.GetSellerProfile;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Moq;

namespace Application.Tests.Sellers;

// A banned seller must read as "not found," not as a profile with an empty listings section —
// same "treat it as gone" reasoning DeactivateAccountCommand uses elsewhere. Getting this
// backwards would leave a banned seller's public storefront browsable.
public class GetSellerProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_BannedSeller_ThrowsNotFoundEvenThoughRecordExists()
    {
        var seller = new User { Id = 10, Name = "Продавец", IsBanned = true };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        var handler = new GetSellerProfileQueryHandler(userRepo.Object, Mock.Of<ISiteRepository>(), Mock.Of<ISiteReviewRepository>());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetSellerProfileQuery(10), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownSeller_ThrowsNotFound()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = new GetSellerProfileQueryHandler(userRepo.Object, Mock.Of<ISiteRepository>(), Mock.Of<ISiteReviewRepository>());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetSellerProfileQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActiveSeller_ReturnsProfileWithRatingSummary()
    {
        var seller = new User { Id = 10, Name = "Продавец", IsBanned = false };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.GetActiveByUserAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Site> { TestBuilders.Site(sellerId: 10) });

        var reviewRepo = new Mock<ISiteReviewRepository>();
        reviewRepo.Setup(r => r.GetSummaryBySellerAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((4.5m, 12));

        var handler = new GetSellerProfileQueryHandler(userRepo.Object, siteRepo.Object, reviewRepo.Object);
        var result = await handler.Handle(new GetSellerProfileQuery(10), CancellationToken.None);

        Assert.Equal(4.5m, result.AverageRating);
        Assert.Equal(12, result.ReviewsCount);
        Assert.Single(result.ActiveSites);
    }
}
