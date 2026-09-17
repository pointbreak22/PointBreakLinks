using Application.CQRS.Wallet.DTOs;
using MediatR;

namespace Application.CQRS.Wallet.Commands.RequestWithdrawal;

public record RequestWithdrawalCommand(int UserId, decimal Amount, string PayoutDetails) : IRequest<WithdrawalRequestDto>;
