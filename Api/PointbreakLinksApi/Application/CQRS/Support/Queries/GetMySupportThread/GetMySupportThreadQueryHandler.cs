using Application.CQRS.Support.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Support.Queries.GetMySupportThread;

public class GetMySupportThreadQueryHandler(ISupportTicketRepository ticketRepository, ISupportMessageRepository messageRepository)
    : IRequestHandler<GetMySupportThreadQuery, IReadOnlyList<SupportMessageDto>>
{
    public async Task<IReadOnlyList<SupportMessageDto>> Handle(GetMySupportThreadQuery request, CancellationToken cancellationToken)
    {
        // Deliberately does NOT create a ticket just because the user opened the page — see
        // ISupportTicketRepository's comment. No ticket yet just means no messages yet.
        var ticket = await ticketRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (ticket == null)
        {
            return [];
        }

        var messages = await messageRepository.GetByTicketAsync(ticket.Id, cancellationToken);

        await messageRepository.MarkStaffMessagesReadAsync(ticket.Id, cancellationToken);

        return messages.Select(SupportMessageDto.FromEntity).ToList();
    }
}
