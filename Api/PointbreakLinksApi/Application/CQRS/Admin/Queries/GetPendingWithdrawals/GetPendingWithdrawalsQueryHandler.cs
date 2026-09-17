using Application.Common;
using Application.CQRS.Admin.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetPendingWithdrawals;

public class GetPendingWithdrawalsQueryHandler(IWithdrawalRequestRepository withdrawalRequestRepository)
    : IRequestHandler<GetPendingWithdrawalsQuery, PagedResult<AdminWithdrawalRequestDto>>
{
    public async Task<PagedResult<AdminWithdrawalRequestDto>> Handle(GetPendingWithdrawalsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await withdrawalRequestRepository.GetPendingAsync(request.Page, request.PerPage, cancellationToken);
        var dtos = items.Select(AdminWithdrawalRequestDto.FromEntity).ToList();
        return PagedResult<AdminWithdrawalRequestDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
