using Application.Common;
using Application.CQRS.Sites.Commands.RequestPublication;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using Moq;

namespace Application.Tests.Sites;

// The buyer side of the wallet transfer (see AcceptOrderCommandHandlerTests for the seller
// side) — FinalPrice is debited at order creation, not at accept/publish (per the handler's own
// comment: there's no reject/cancel path anywhere that would need to reverse it), so an
// insufficient-balance check here is the only thing standing between a buyer and a negative
// wallet balance.
public class RequestPublicationCommandHandlerTests
{
    private static RequestPublicationCommand Command(int siteId = 1, int buyerId = 20, int projectId = 1, decimal priceFinal = 1000m) => new(
        SiteId: siteId,
        BuyerId: buyerId,
        ProjectId: projectId,
        HasLinks: false,
        Links: [],
        TaskDescription: "Разместить ссылку",
        PriceFinal: priceFinal,
        InsuranceType: InsuranceType.None,
        CheckUniqueness: false,
        IsUrgent: false,
        IsExpertArticle: false);

    private static (RequestPublicationCommandHandler Handler, Mock<IWalletRepository> WalletRepo, Mock<IBalanceTransactionRepository> TransactionRepo, Mock<ISiteRepository> SiteRepo, Mock<IPurchasedSiteRepository> OrderRepo)
        CreateHandler(Site site, Project project, Wallet wallet)
    {
        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(r => r.GetByIdAsync(site.Id, It.IsAny<CancellationToken>())).ReturnsAsync(site);

        var projectRepo = new Mock<IProjectRepository>();
        projectRepo.Setup(r => r.GetByIdForOwnerAsync(project.Id, project.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(r => r.GetOrCreateAsync(wallet.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);

        var orderRepo = new Mock<IPurchasedSiteRepository>();
        PurchasedSite? captured = null;
        orderRepo.Setup(r => r.AddAsync(It.IsAny<PurchasedSite>(), It.IsAny<CancellationToken>()))
            .Callback<PurchasedSite, CancellationToken>((o, _) =>
            {
                o.Site = site;
                o.Buyer = new User { Id = o.BuyerId, Name = "Покупатель" };
                o.Status = new Status { Id = o.StatusId, Name = "application", Description = "Заявка" };
                captured = o;
            })
            .Returns(Task.CompletedTask);
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => captured);

        var paymentSettingRepo = new Mock<IPaymentSettingRepository>();
        paymentSettingRepo.Setup(r => r.GetOrCreateAsync(It.IsAny<InsuranceType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentSetting { Id = 1 });

        var transactionRepo = new Mock<IBalanceTransactionRepository>();

        var handler = new RequestPublicationCommandHandler(
            siteRepo.Object, projectRepo.Object, orderRepo.Object, paymentSettingRepo.Object, walletRepo.Object,
            transactionRepo.Object, Mock.Of<IDynamicStatsRefresher>(), Mock.Of<INotificationPusher>(),
            Mock.Of<IPurchasedSiteEventRepository>(), Mock.Of<INotificationRepository>(), Mock.Of<IUserRepository>(),
            Mock.Of<IEmailSender>(), Mock.Of<INotificationPreferenceRepository>());

        return (handler, walletRepo, transactionRepo, siteRepo, orderRepo);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ThrowsConflictAndDoesNotCreateOrder()
    {
        var site = TestBuilders.Site(id: 1, sellerId: 10);
        var project = new Project { Id = 1, UserId = 20 };
        var wallet = TestBuilders.Wallet(userId: 20, balance: 100m);
        var (handler, _, _, _, orderRepo) = CreateHandler(site, project, wallet);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(Command(priceFinal: 1000m), CancellationToken.None));

        orderRepo.Verify(r => r.AddAsync(It.IsAny<PurchasedSite>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(100m, wallet.Balance); // untouched
    }

    [Fact]
    public async Task Handle_SufficientBalance_DebitsBuyerAndIncrementsSoldCount()
    {
        var site = TestBuilders.Site(id: 1, sellerId: 10);
        var project = new Project { Id = 1, UserId = 20 };
        var wallet = TestBuilders.Wallet(userId: 20, balance: 5000m);
        var (handler, walletRepo, transactionRepo, _, orderRepo) = CreateHandler(site, project, wallet);

        BalanceTransaction? capturedTransaction = null;
        transactionRepo.Setup(r => r.AddAsync(It.IsAny<BalanceTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<BalanceTransaction, CancellationToken>((t, _) => capturedTransaction = t)
            .Returns(Task.CompletedTask);

        var result = await handler.Handle(Command(priceFinal: 1000m), CancellationToken.None);

        Assert.Equal(4000m, wallet.Balance);
        Assert.Equal(1, site.SoldCount);
        Assert.Equal(1000m, result.PriceFinal);
        Assert.NotNull(capturedTransaction);
        Assert.Equal(BalanceTransactionType.Purchase, capturedTransaction!.Type);
        Assert.Equal(-1000m, capturedTransaction.Amount); // negative — a debit, unlike TopUp/Sale's positive credit
        orderRepo.Verify(r => r.AddAsync(It.IsAny<PurchasedSite>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
