using MediatR;

namespace Application.CQRS.Wallet.Commands.TopUpBalance;

public record TopUpBalanceCommand(int UserId, decimal Amount, string PaymentMethod) : IRequest<decimal>;
