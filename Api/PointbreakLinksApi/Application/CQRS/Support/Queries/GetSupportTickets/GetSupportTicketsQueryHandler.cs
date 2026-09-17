using Application.Common;
using Application.CQRS.Support.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Support.Queries.GetSupportTickets;

public class GetSupportTicketsQueryHandler(ISupportTicketRepository ticketRepository) : IRequestHandler<GetSupportTicketsQuery, PagedResult<SupportTicketSummaryDto>>
{
    public async Task<PagedResult<SupportTicketSummaryDto>> Handle(GetSupportTicketsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await ticketRepository.GetAllForStaffAsync(request.Page, request.PerPage, cancellationToken);

        var dtos = items
            .Select(t => new SupportTicketSummaryDto(t.Id, t.UserName, t.LastMessageText, t.LastMessageAt.ToString("dd.MM.yyyy HH:mm"), t.UnreadCount))
            .ToList();

        return PagedResult<SupportTicketSummaryDto>.Create(dtos, total, request.Page, request.PerPage);
    }
}
