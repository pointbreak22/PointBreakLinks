using Domain.Entities;

namespace Application.CQRS.Wallet.DTOs;

public record WithdrawalRequestDto(
    int Id,
    decimal Amount,
    string PayoutDetails,
    string Status,
    string RequestedAt,
    string? ProcessedAt,
    string? AdminComment)
{
    public static WithdrawalRequestDto FromEntity(WithdrawalRequest request) => new(
        request.Id,
        request.Amount,
        request.PayoutDetails,
        request.Status.ToString(),
        request.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        request.ProcessedAt?.ToString("dd.MM.yyyy HH:mm"),
        request.AdminComment);
}
