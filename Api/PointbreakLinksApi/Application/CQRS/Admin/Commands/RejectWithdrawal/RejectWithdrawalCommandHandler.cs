using Application.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Admin.Commands.RejectWithdrawal;

// Reverses the debit RequestWithdrawalCommandHandler made at request time — same Refund reuse
// pattern as CancelOrderCommandHandler/DeclineOrderCommandHandler.
public class RejectWithdrawalCommandHandler(
    IWithdrawalRequestRepository withdrawalRequestRepository,
    IWalletRepository walletRepository,
    IBalanceTransactionRepository transactionRepository,
    INotificationRepository notificationRepository,
    IEmailSender emailSender,
    ILogger<RejectWithdrawalCommandHandler> logger)
    : IRequestHandler<RejectWithdrawalCommand>
{
    public async Task Handle(RejectWithdrawalCommand request, CancellationToken cancellationToken)
    {
        var withdrawal = await withdrawalRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
                          ?? throw new NotFoundException(nameof(Domain.Entities.WithdrawalRequest), request.RequestId);

        if (withdrawal.Status != WithdrawalRequestStatus.Pending)
        {
            throw new ConflictException("Заявка уже обработана.");
        }

        withdrawal.Status = WithdrawalRequestStatus.Rejected;
        withdrawal.ProcessedAt = DateTime.UtcNow;
        withdrawal.AdminComment = request.Comment;
        await withdrawalRequestRepository.SaveChangesAsync(cancellationToken);

        var wallet = await walletRepository.GetOrCreateAsync(withdrawal.UserId, cancellationToken);
        wallet.Balance += withdrawal.Amount;
        await walletRepository.SaveChangesAsync(cancellationToken);
        await transactionRepository.AddAsync(
            new BalanceTransaction
            {
                UserId = withdrawal.UserId,
                Type = BalanceTransactionType.Refund,
                Amount = withdrawal.Amount,
                Description = "Возврат по отклонённой заявке на вывод средств",
            },
            cancellationToken);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Withdrawal request {RequestId} for {Amount} rejected (user {UserId}), funds refunded to wallet. Comment: {Comment}",
            withdrawal.Id, withdrawal.Amount, withdrawal.UserId, request.Comment ?? "(none)");

        await notificationRepository.AddAsync(
            new Notification { UserId = withdrawal.UserId, Message = $"Заявка на вывод {withdrawal.Amount} ₽ отклонена, средства возвращены на баланс" },
            cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            withdrawal.User.Email,
            "Заявка на вывод средств отклонена",
            $"""
             <p>Здравствуйте, {withdrawal.User.Name}!</p>
             <p>Ваша заявка на вывод {withdrawal.Amount} ₽ отклонена, средства возвращены на баланс.</p>
             {(string.IsNullOrWhiteSpace(request.Comment) ? "" : $"<p>Комментарий администратора: {request.Comment}</p>")}
             """,
            cancellationToken);
    }
}
