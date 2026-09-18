using Application.CQRS.Wallet.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Commands.RequestWithdrawal;

// Debits the wallet the moment a request is created (same "hold the funds immediately"
// reasoning as RequestPublicationCommandHandler's Purchase debit) — a rejected request reverses
// this with a Refund transaction rather than waiting for admin action to touch the balance at
// all, so the user can never spend money they've already asked to withdraw.
public class RequestWithdrawalCommandHandler(
    IWalletRepository walletRepository,
    IBalanceTransactionRepository transactionRepository,
    IWithdrawalRequestRepository withdrawalRequestRepository)
    : IRequestHandler<RequestWithdrawalCommand, WithdrawalRequestDto>
{
    public async Task<WithdrawalRequestDto> Handle(RequestWithdrawalCommand request, CancellationToken cancellationToken)
    {
        // Amount > 0 and PayoutDetails-not-empty are enforced by
        // RequestWithdrawalCommandValidator before this handler ever runs.
        var wallet = await walletRepository.GetOrCreateAsync(request.UserId, cancellationToken);
        if (wallet.Balance < request.Amount)
        {
            throw new ConflictException("Недостаточно средств на балансе.");
        }

        wallet.Balance -= request.Amount;
        await walletRepository.SaveChangesAsync(cancellationToken);

        var withdrawalRequest = new WithdrawalRequest
        {
            UserId = request.UserId,
            Amount = request.Amount,
            PayoutDetails = request.PayoutDetails.Trim(),
            Status = WithdrawalRequestStatus.Pending,
        };
        await withdrawalRequestRepository.AddAsync(withdrawalRequest, cancellationToken);
        await withdrawalRequestRepository.SaveChangesAsync(cancellationToken);

        await transactionRepository.AddAsync(
            new BalanceTransaction
            {
                UserId = request.UserId,
                Type = BalanceTransactionType.Withdrawal,
                Amount = -request.Amount,
                Description = "Заявка на вывод средств",
            },
            cancellationToken);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        return WithdrawalRequestDto.FromEntity(withdrawalRequest);
    }
}
