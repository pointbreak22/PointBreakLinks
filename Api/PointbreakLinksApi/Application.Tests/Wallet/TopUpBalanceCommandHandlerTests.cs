using Application.CQRS.Wallet.Commands.TopUpBalance;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using Moq;

namespace Application.Tests.WalletTests;

public class TopUpBalanceCommandHandlerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Handle_NonPositiveAmount_ThrowsConflict(decimal amount)
    {
        var handler = new TopUpBalanceCommandHandler(Mock.Of<IWalletRepository>(), Mock.Of<IBalanceTransactionRepository>());

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new TopUpBalanceCommand(UserId: 1, amount, PaymentMethodNames.VisaMastercard), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownPaymentMethod_ThrowsConflict()
    {
        var handler = new TopUpBalanceCommandHandler(Mock.Of<IWalletRepository>(), Mock.Of<IBalanceTransactionRepository>());

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new TopUpBalanceCommand(UserId: 1, 500m, "bitcoin"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidTopUp_IncreasesBalanceAndRecordsLedgerEntry()
    {
        var wallet = TestBuilders.Wallet(userId: 1, balance: 200m);
        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(r => r.GetOrCreateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);

        var transactionRepo = new Mock<IBalanceTransactionRepository>();
        BalanceTransaction? captured = null;
        transactionRepo.Setup(r => r.AddAsync(It.IsAny<BalanceTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<BalanceTransaction, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        var handler = new TopUpBalanceCommandHandler(walletRepo.Object, transactionRepo.Object);

        var newBalance = await handler.Handle(new TopUpBalanceCommand(UserId: 1, 300m, PaymentMethodNames.Mir), CancellationToken.None);

        Assert.Equal(500m, newBalance);
        Assert.Equal(500m, wallet.Balance);
        Assert.NotNull(captured);
        Assert.Equal(BalanceTransactionType.TopUp, captured!.Type);
        Assert.Equal(300m, captured.Amount); // positive — TopUp/Sale credit, unlike Purchase's negative amount
        Assert.Equal(PaymentMethodNames.Mir, captured.PaymentMethod);
    }
}
