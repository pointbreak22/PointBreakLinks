using Application.Common;
using Application.CQRS.Support.DTOs;
using MediatR;

namespace Application.CQRS.Support.Queries.GetSupportTickets;

public record GetSupportTicketsQuery(int Page, int PerPage) : IRequest<PagedResult<SupportTicketSummaryDto>>;
