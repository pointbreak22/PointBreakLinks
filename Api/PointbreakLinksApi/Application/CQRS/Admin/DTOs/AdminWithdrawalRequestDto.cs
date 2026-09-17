using Domain.Entities;

namespace Application.CQRS.Admin.DTOs;

// Pending-only admin queue row — see IWithdrawalRequestRepository.GetPendingAsync.
public record AdminWithdrawalRequestDto(int Id, string UserName, string UserEmail, decimal Amount, string PayoutDetails, string RequestedAt)
{
    public static AdminWithdrawalRequestDto FromEntity(WithdrawalRequest request) => new(
        request.Id,
        request.User.Name,
        request.User.Email,
        request.Amount,
        request.PayoutDetails,
        request.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
}
