using Application.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Commands.ApproveWithdrawal;

public class ApproveWithdrawalCommandHandler(
    IWithdrawalRequestRepository withdrawalRequestRepository,
    INotificationRepository notificationRepository,
    IEmailSender emailSender)
    : IRequestHandler<ApproveWithdrawalCommand>
{
    public async Task Handle(ApproveWithdrawalCommand request, CancellationToken cancellationToken)
    {
        var withdrawal = await withdrawalRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
                          ?? throw new NotFoundException(nameof(WithdrawalRequest), request.RequestId);

        if (withdrawal.Status != WithdrawalRequestStatus.Pending)
        {
            throw new ConflictException("Заявка уже обработана.");
        }

        withdrawal.Status = WithdrawalRequestStatus.Approved;
        withdrawal.ProcessedAt = DateTime.UtcNow;
        await withdrawalRequestRepository.SaveChangesAsync(cancellationToken);

        await notificationRepository.AddAsync(
            new Notification { UserId = withdrawal.UserId, Message = $"Заявка на вывод {withdrawal.Amount} ₽ одобрена, средства отправлены" },
            cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            withdrawal.User.Email,
            "Заявка на вывод средств одобрена",
            $"""
             <p>Здравствуйте, {withdrawal.User.Name}!</p>
             <p>Ваша заявка на вывод {withdrawal.Amount} ₽ одобрена, средства отправлены на указанные реквизиты.</p>
             """,
            cancellationToken);
    }
}
