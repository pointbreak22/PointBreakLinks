using Application.CQRS.Sites.Queries.GetPurchasedSiteEvents;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Moq;

namespace Application.Tests.Sites;

// The handler tries the buyer lookup, then falls back to the seller lookup — either party to
// the order can view its timeline. A regression that collapses this to a single-role check
// would silently 404 one entire side of every order's history view.
public class GetPurchasedSiteEventsQueryHandlerTests
{
    private const int BuyerId = 20;
    private const int SellerId = 10;
    private const int OrderId = 1;

    private static Mock<IPurchasedSiteRepository> OrderRepoWhereOnly(bool buyerMatches, bool sellerMatches)
    {
        var order = TestBuilders.Order(id: OrderId, buyerId: BuyerId);
        var repo = new Mock<IPurchasedSiteRepository>();
        repo.Setup(r => r.GetByIdForBuyerAsync(OrderId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buyerMatches ? order : null);
        repo.Setup(r => r.GetByIdForSellerAsync(OrderId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sellerMatches ? order : null);
        return repo;
    }

    [Fact]
    public async Task Handle_Buyer_CanViewTimeline()
    {
        var orderRepo = OrderRepoWhereOnly(buyerMatches: true, sellerMatches: false);
        var eventRepo = new Mock<IPurchasedSiteEventRepository>();
        eventRepo.Setup(r => r.GetByPurchasedSiteAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PurchasedSiteEvent>());

        var handler = new GetPurchasedSiteEventsQueryHandler(orderRepo.Object, eventRepo.Object);
        var result = await handler.Handle(new GetPurchasedSiteEventsQuery(OrderId, ViewerId: BuyerId), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_Seller_CanViewTimeline()
    {
        var orderRepo = OrderRepoWhereOnly(buyerMatches: false, sellerMatches: true);
        var eventRepo = new Mock<IPurchasedSiteEventRepository>();
        eventRepo.Setup(r => r.GetByPurchasedSiteAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PurchasedSiteEvent>());

        var handler = new GetPurchasedSiteEventsQueryHandler(orderRepo.Object, eventRepo.Object);
        var result = await handler.Handle(new GetPurchasedSiteEventsQuery(OrderId, ViewerId: SellerId), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_UnrelatedUser_ThrowsNotFound()
    {
        var orderRepo = OrderRepoWhereOnly(buyerMatches: false, sellerMatches: false);
        var eventRepo = new Mock<IPurchasedSiteEventRepository>();

        var handler = new GetPurchasedSiteEventsQueryHandler(orderRepo.Object, eventRepo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetPurchasedSiteEventsQuery(OrderId, ViewerId: 999), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsEventsOrderedChronologically()
    {
        var orderRepo = OrderRepoWhereOnly(buyerMatches: true, sellerMatches: false);
        var eventRepo = new Mock<IPurchasedSiteEventRepository>();
        eventRepo.Setup(r => r.GetByPurchasedSiteAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PurchasedSiteEvent>
        {
            new() { Id = 2, Description = "second", CreatedAt = new DateTime(2026, 1, 2) },
            new() { Id = 1, Description = "first", CreatedAt = new DateTime(2026, 1, 1) },
        });

        var handler = new GetPurchasedSiteEventsQueryHandler(orderRepo.Object, eventRepo.Object);
        var result = await handler.Handle(new GetPurchasedSiteEventsQuery(OrderId, ViewerId: BuyerId), CancellationToken.None);

        Assert.Equal(["first", "second"], result.Select(e => e.Description));
    }
}
