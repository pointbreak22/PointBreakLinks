using Application.Common;
using Application.CQRS.Wallet.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Wallet.Queries.GetMyWithdrawalRequests;

public class GetMyWithdrawalRequestsQueryHandler(IWithdrawalRequestRepository withdrawalRequestRepository)
    : IRequestHandler<GetMyWithdrawalRequestsQuery, PagedResult<WithdrawalRequestDto>>
{
    public async Task<PagedResult<WithdrawalRequestDto>> Handle(GetMyWithdrawalRequestsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await withdrawalRequestRepository.GetByUserAsync(request.UserId, request.Page, request.PerPage, cancellationToken);
        var dtos = items.Select(WithdrawalRequestDto.FromEntity).ToList();
        return PagedResult<WithdrawalRequestDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
