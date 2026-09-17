using Application.Common;
using Application.CQRS.Admin.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetAdminTransactions;

public class GetAdminTransactionsQueryHandler(IBalanceTransactionRepository transactionRepository)
    : IRequestHandler<GetAdminTransactionsQuery, PagedResult<AdminTransactionDto>>
{
    public async Task<PagedResult<AdminTransactionDto>> Handle(GetAdminTransactionsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await transactionRepository.GetAllPaginatedAsync(request.Page, request.PerPage, cancellationToken);

        var dtos = items
            .Select(t => new AdminTransactionDto(t.Id, t.UserName, t.Type, t.Amount, t.Description, t.CreatedAt.ToString("dd.MM.yyyy HH:mm")))
            .ToList();

        return PagedResult<AdminTransactionDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
