using Application.Common;
using Application.CQRS.Wallet.DTOs;
using MediatR;

namespace Application.CQRS.Wallet.Queries.GetMyTransactions;

public record GetMyTransactionsQuery(int UserId, int Page, int PerPage) : IRequest<PagedResult<BalanceTransactionDto>>;
