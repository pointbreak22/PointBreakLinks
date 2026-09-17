using Application.Common;
using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetAdminTransactions;

public record GetAdminTransactionsQuery(int Page, int PerPage) : IRequest<PagedResult<AdminTransactionDto>>;
