using MediatR;

namespace Application.CQRS.Wallet.Queries.GetMyBalance;

public record GetMyBalanceQuery(int UserId) : IRequest<decimal>;
