using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Commands.TopUpBalance;

// No real payment gateway is integrated anywhere in this project (see PROJECT_MAP.md's
// "Известные ограничения") — FOXLinks' own balance-topup-modal.vue is orphaned, never imported
// anywhere, so there's no reference flow to port either. This credits the balance instantly
// instead of going through a fake "processing" delay or a stubbed provider callback: the ledger
// (BalanceTransaction), the running total (Wallet.Balance), and everything downstream that
// spends it (RequestPublicationCommandHandler) are 100% real and enforced — only the payment
// collection step itself is missing. Swap this handler's instant credit for a real provider
// webhook/return handler later; nothing else in the wallet needs to change. PaymentMethod is
// recorded for real even though nothing processes it yet — see PaymentMethodNames.cs.
public class TopUpBalanceCommandHandler(IWalletRepository walletRepository, IBalanceTransactionRepository transactionRepository)
    : IRequestHandler<TopUpBalanceCommand, decimal>
{
    private static readonly Dictionary<string, string> MethodDescriptions = new()
    {
        [PaymentMethodNames.VisaMastercard] = "Пополнение баланса картой Visa/Mastercard",
        [PaymentMethodNames.Mir] = "Пополнение баланса картой МИР",
    };

    public async Task<decimal> Handle(TopUpBalanceCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ConflictException("Сумма пополнения должна быть больше нуля.");
        }

        if (!MethodDescriptions.TryGetValue(request.PaymentMethod, out var description))
        {
            throw new ConflictException("Неизвестный способ оплаты.");
        }

        var wallet = await walletRepository.GetOrCreateAsync(request.UserId, cancellationToken);
        wallet.Balance += request.Amount;
        await walletRepository.SaveChangesAsync(cancellationToken);

        await transactionRepository.AddAsync(
            new BalanceTransaction
            {
                UserId = request.UserId,
                Type = BalanceTransactionType.TopUp,
                Amount = request.Amount,
                Description = description,
                PaymentMethod = request.PaymentMethod,
            },
            cancellationToken);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        return wallet.Balance;
    }
}
