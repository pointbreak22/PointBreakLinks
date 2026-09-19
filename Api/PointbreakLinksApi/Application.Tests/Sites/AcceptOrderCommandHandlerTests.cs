using Application.Common;
using Application.CQRS.Sites.Commands.AcceptOrder;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Sites;

// The seller side of the wallet transfer (see RequestPublicationCommandHandlerTests for the
// buyer side) — per the handler's own comment, the seller is credited at accept, not at order
// creation, so paying out before the seller has agreed to fulfil the order is exactly the bug
// this ordering prevents.
public class AcceptOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_Accept_CreditsSellerAndMovesOrderToWork()
    {
        const int sellerId = 10;
        var site = TestBuilders.Site(id: 1, sellerId: sellerId);
        var order = TestBuilders.Order(id: 5, statusId: 1, statusName: "application", finalPrice: 1500m, site: site);

        var orderRepo = new Mock<IPurchasedSiteRepository>();
        orderRepo.Setup(r => r.GetByIdForSellerAsync(order.Id, sellerId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var wallet = TestBuilders.Wallet(userId: sellerId, balance: 0m);
        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(r => r.GetOrCreateAsync(sellerId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);

        var transactionRepo = new Mock<IBalanceTransactionRepository>();
        BalanceTransaction? capturedTransaction = null;
        transactionRepo.Setup(r => r.AddAsync(It.IsAny<BalanceTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<BalanceTransaction, CancellationToken>((t, _) => capturedTransaction = t)
            .Returns(Task.CompletedTask);

        var handler = new AcceptOrderCommandHandler(
            orderRepo.Object, walletRepo.Object, transactionRepo.Object, Mock.Of<IDynamicStatsRefresher>(),
            Mock.Of<INotificationPusher>(), Mock.Of<IPurchasedSiteEventRepository>(), Mock.Of<INotificationRepository>(),
            Mock.Of<IUserRepository>(), Mock.Of<IEmailSender>(), Mock.Of<INotificationPreferenceRepository>(),
            Mock.Of<ILogger<AcceptOrderCommandHandler>>());

        var result = await handler.Handle(new AcceptOrderCommand(order.Id, sellerId), CancellationToken.None);

        Assert.Equal(4, order.StatusId); // "work"
        Assert.Equal(1500m, wallet.Balance);
        Assert.NotNull(capturedTransaction);
        Assert.Equal(BalanceTransactionType.Sale, capturedTransaction!.Type);
        Assert.Equal(1500m, capturedTransaction.Amount); // positive — a credit, unlike Purchase's negative amount
        Assert.Equal(order.Id, capturedTransaction.PurchasedSiteId);
        Assert.Equal(1500m, result.PriceFinal);
    }
}
