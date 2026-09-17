using Application.CQRS.Support.DTOs;
using MediatR;

namespace Application.CQRS.Support.Queries.GetSupportTicketMessages;

public record GetSupportTicketMessagesQuery(int TicketId) : IRequest<IReadOnlyList<SupportMessageDto>>;
