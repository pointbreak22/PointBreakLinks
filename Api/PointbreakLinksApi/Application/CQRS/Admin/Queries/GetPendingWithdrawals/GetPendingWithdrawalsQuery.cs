using Application.Common;
using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetPendingWithdrawals;

public record GetPendingWithdrawalsQuery(int Page, int PerPage) : IRequest<PagedResult<AdminWithdrawalRequestDto>>;
