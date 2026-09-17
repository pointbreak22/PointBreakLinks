using Application.CQRS.Support.DTOs;
using Domain.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Support.Queries.GetSupportTicketMessages;

public class GetSupportTicketMessagesQueryHandler(ISupportTicketRepository ticketRepository, ISupportMessageRepository messageRepository)
    : IRequestHandler<GetSupportTicketMessagesQuery, IReadOnlyList<SupportMessageDto>>
{
    public async Task<IReadOnlyList<SupportMessageDto>> Handle(GetSupportTicketMessagesQuery request, CancellationToken cancellationToken)
    {
        _ = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw new NotFoundException("SupportTicket", request.TicketId);

        var messages = await messageRepository.GetByTicketAsync(request.TicketId, cancellationToken);

        // Any admin/moderator viewing counts as "staff has seen it" — no per-staff-member read
        // state (see ISupportMessageRepository's comment).
        await messageRepository.MarkUserMessagesReadAsync(request.TicketId, cancellationToken);

        return messages.Select(SupportMessageDto.FromEntity).ToList();
    }
}
