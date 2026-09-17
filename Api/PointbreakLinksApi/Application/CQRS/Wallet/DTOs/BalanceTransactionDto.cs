using Domain.Entities;
using Domain.Enums;

namespace Application.CQRS.Wallet.DTOs;

public record BalanceTransactionDto(int Id, BalanceTransactionType Type, decimal Amount, string Description, string? PaymentMethod, string CreatedAt)
{
    public static BalanceTransactionDto FromEntity(BalanceTransaction transaction) => new(
        transaction.Id,
        transaction.Type,
        transaction.Amount,
        transaction.Description,
        transaction.PaymentMethod,
        transaction.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
}
