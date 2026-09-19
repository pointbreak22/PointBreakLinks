using Application.CQRS.Wallet.Commands.TopUpBalance;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.WalletTests;

// Amount > 0 / PaymentMethod-is-known are enforced by TopUpBalanceCommandValidator (see
// TopUpBalanceCommandValidatorTests) via the MediatR pipeline, not by this handler directly —
// so those cases are no longer meaningful to test against the handler in isolation.
public class TopUpBalanceCommandHandlerTests
{
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

        var handler = new TopUpBalanceCommandHandler(walletRepo.Object, transactionRepo.Object, Mock.Of<ILogger<TopUpBalanceCommandHandler>>());

        var newBalance = await handler.Handle(new TopUpBalanceCommand(UserId: 1, 300m, PaymentMethodNames.Mir), CancellationToken.None);

        Assert.Equal(500m, newBalance);
        Assert.Equal(500m, wallet.Balance);
        Assert.NotNull(captured);
        Assert.Equal(BalanceTransactionType.TopUp, captured!.Type);
        Assert.Equal(300m, captured.Amount); // positive — TopUp/Sale credit, unlike Purchase's negative amount
        Assert.Equal(PaymentMethodNames.Mir, captured.PaymentMethod);
    }
}
