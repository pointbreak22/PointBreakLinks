using Application.Common;
using Application.CQRS.Wallet.DTOs;
using MediatR;

namespace Application.CQRS.Wallet.Queries.GetMyWithdrawalRequests;

public record GetMyWithdrawalRequestsQuery(int UserId, int Page, int PerPage) : IRequest<PagedResult<WithdrawalRequestDto>>;
