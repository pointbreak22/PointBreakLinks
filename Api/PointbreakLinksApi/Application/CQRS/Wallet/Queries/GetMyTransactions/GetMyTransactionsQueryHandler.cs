using Application.Common;
using Application.CQRS.Wallet.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Queries.GetMyTransactions;

public class GetMyTransactionsQueryHandler(IBalanceTransactionRepository transactionRepository)
    : IRequestHandler<GetMyTransactionsQuery, PagedResult<BalanceTransactionDto>>
{
    public async Task<PagedResult<BalanceTransactionDto>> Handle(GetMyTransactionsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await transactionRepository.GetByUserAsync(request.UserId, request.Page, request.PerPage, cancellationToken);
        var dtos = items.Select(BalanceTransactionDto.FromEntity).ToList();
        return PagedResult<BalanceTransactionDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
